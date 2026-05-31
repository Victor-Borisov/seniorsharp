using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SeniorSharp.Api;
using SeniorSharp.Contracts;
using SeniorSharp.Llm;
using SeniorSharp.Orchestration;
using SeniorSharp.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration -------------------------------------------------------
var postgres = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");

// --- Module DI wiring ----------------------------------------------------
// Persistence: register AppDbContext on Npgsql per the Api contract.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgres));
builder.Services.AddScoped<GraphSeeder>();

// LLM provider abstraction + Anthropic adapter.
// Anthropic:Model / Anthropic:ApiKey are bound by AddSeniorSharpLlm (key via env).
builder.Services.AddSeniorSharpLlm(builder.Configuration);

// Orchestration: FSM-driven orchestrator + questioner/classifier/scorer roles + prompt provider.
builder.Services.AddSeniorSharpOrchestration(builder.Configuration);

// Voice I/O (OpenAI STT/TTS) — typed HttpClient + service reading Voice:ApiKey.
builder.Services.AddHttpClient<SpeechService>();

// --- Observability: OpenTelemetry tracing -> OTLP (Langfuse) -------------
var otlpEndpoint = builder.Configuration["Otlp:Endpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: builder.Configuration["Otlp:ServiceName"] ?? "seniorsharp-api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // LLM round-trips (structured/text completions) emit spans under this source,
            // so each scoring/classification run is traced into Langfuse.
            .AddSource(AnthropicLlmClient.ActivitySourceName)
            .AddOtlpExporter(o =>
            {
                // TODO(M1): point at the Langfuse OTLP ingest endpoint and add the
                // Basic auth header (public/secret key) once Langfuse is provisioned.
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    o.Endpoint = new Uri(otlpEndpoint);
                }
            });
    });

builder.Services.AddProblemDetails();

var app = builder.Build();

// --- Seed mode: `dotnet run -- seed` upserts the skill graph and exits ----
// Idempotent (GraphSeeder upserts by node Id). Used for fresh DBs / CI; the
// graph itself lives in the closed-content folder, path via Content:GraphPath.
if (args.Contains("seed"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<GraphSeeder>();
    var graphPath = app.Configuration["Content:GraphPath"]
        ?? throw new InvalidOperationException("Missing Content:GraphPath.");
    var count = await seeder.SeedFromFileAsync(Path.GetFullPath(graphPath));
    app.Logger.LogInformation("Seeded {Count} skill nodes from {Path}.", count, graphPath);
    return;
}

// --- Score-demo mode: `dotnet run -- score-demo` scores a fixture transcript and prints the verdict.
// This is the M1 "go" check: one real forced-tool-use call against Claude + an OTel/Langfuse span.
if (args.Contains("score-demo"))
{
    using var scope = app.Services.CreateScope();
    var scorer = scope.ServiceProvider.GetRequiredService<IScorer>();

    var transcriptPath = Path.GetFullPath(app.Configuration["Content:DemoTranscriptPath"]
        ?? "../../fixtures/transcript-senior.json");
    var criteriaPath = Path.GetFullPath(app.Configuration["Content:CriteriaPath"]
        ?? "../../content/criteria.md");

    var request = new ScorerRequest(
        TranscriptJson: await File.ReadAllTextAsync(transcriptPath),
        CriteriaJson: await File.ReadAllTextAsync(criteriaPath),
        Axes: new[] { "TechnicalDepth", "Architecture", "ProductionMaturity", "Communication" });

    app.Logger.LogInformation("Scoring demo transcript {Path}...", transcriptPath);
    var verdict = await scorer.ScoreAsync(request);

    Console.WriteLine(JsonSerializer.Serialize(verdict, new JsonSerializerOptions { WriteIndented = true }));
    return;
}

// --- Deep-dive demo: `dotnet run -- deepdive-demo [level]` runs the full adaptive loop with a
// simulated candidate of the given level (default senior). This is the M2 "go" check.
if (args.Contains("deepdive-demo"))
{
    var level = args.SkipWhile(a => a != "deepdive-demo").Skip(1).FirstOrDefault() ?? "senior";

    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var orchestrator = sp.GetRequiredService<IInterviewOrchestrator>();
    var candidate = sp.GetRequiredService<ICandidate>();
    var db = sp.GetRequiredService<AppDbContext>();

    var start = await orchestrator.StartAsync(new StartInterviewRequest($"demo-{level}"));
    var sessionId = start.SessionId;
    var question = start.FirstQuestion;
    Console.WriteLine($"=== Deep-dive demo (simulated {level} candidate) — session {sessionId} ===");

    var done = false;
    var n = 0;
    while (!done)
    {
        n++;

        // Fetch the node the open question targets, to ground the simulated answer.
        var round = await db.Rounds.AsNoTracking()
            .Where(r => r.SessionId == sessionId && r.Status == "open")
            .OrderBy(r => r.Order).LastOrDefaultAsync();
        var nodeCtx = string.Empty;
        if (round?.PendingSkillId is { } sk)
        {
            var node = await db.SkillNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sk);
            if (node is not null)
                nodeCtx = $"{node.Title}: {node.Description}\nSenior signal: {node.SeniorSignal}";
        }

        Console.WriteLine($"\nQ{n}: {question}");
        var answer = await candidate.AnswerAsync(question, nodeCtx, level);
        Console.WriteLine($"A{n}: {answer}");

        var res = await orchestrator.SubmitAnswerAsync(new SubmitAnswerRequest(sessionId, answer));
        done = res.RoundComplete;
        question = res.NextQuestion ?? string.Empty;
    }

    var mastery = await db.SkillMasteries.AsNoTracking()
        .Where(m => m.SessionId == sessionId).ToListAsync();
    Console.WriteLine("\n=== Mastery (recognition / application / depth) ===");
    foreach (var m in mastery)
        Console.WriteLine($"  {m.SkillId,-40} R={m.Recognition:F2}  A={m.Application:F2}  D={m.Depth:F2}");
    return;
}

// --- Full interview demo: `dotnet run -- interview-demo [level]` runs all MVP rounds (discovery ->
// deep-dive -> system-design) with a simulated candidate, then scores and prints the verdict. M3 "go".
if (args.Contains("interview-demo"))
{
    var level = args.SkipWhile(a => a != "interview-demo").Skip(1).FirstOrDefault() ?? "senior";

    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var orchestrator = sp.GetRequiredService<IInterviewOrchestrator>();
    var candidate = sp.GetRequiredService<ICandidate>();
    var db = sp.GetRequiredService<AppDbContext>();

    var start = await orchestrator.StartAsync(new StartInterviewRequest($"demo-{level}"));
    var sessionId = start.SessionId;
    var question = start.FirstQuestion;
    Console.WriteLine($"=== Full interview demo (simulated {level}) — session {sessionId} ===");

    var n = 0;
    string? currentRound = null;
    while (question is not null)
    {
        var round = await db.Rounds.AsNoTracking()
            .Where(r => r.SessionId == sessionId && r.Status == "open")
            .OrderBy(r => r.Order).LastOrDefaultAsync();
        if (round is not null && round.Type.ToString() != currentRound)
        {
            currentRound = round.Type.ToString();
            Console.WriteLine($"\n----- Round: {currentRound} -----");
        }

        var nodeCtx = string.Empty;
        if (round?.PendingSkillId is { } sk)
        {
            var node = await db.SkillNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sk);
            if (node is not null)
                nodeCtx = $"{node.Title}: {node.Description}\nSenior signal: {node.SeniorSignal}";
        }

        Console.WriteLine($"\nQ{++n}: {question}");
        var answer = await candidate.AnswerAsync(question, nodeCtx, level);
        Console.WriteLine($"A{n}: {answer}");

        var res = await orchestrator.SubmitAnswerAsync(new SubmitAnswerRequest(sessionId, answer));
        if (!res.RoundComplete)
        {
            question = res.NextQuestion;
            continue;
        }

        // Round finished — advance the FSM (opens the next round, or scores + completes).
        var adv = await orchestrator.AdvanceAsync(sessionId);
        question = adv.NextQuestion;   // null => moved into Scoring/Done
    }

    var verdict = await db.Verdicts.AsNoTracking().FirstOrDefaultAsync(v => v.SessionId == sessionId);
    var axisScores = await db.AxisScores.AsNoTracking().Where(a => a.SessionId == sessionId).ToListAsync();
    Console.WriteLine("\n=== Verdict ===");
    if (verdict is not null)
    {
        Console.WriteLine($"Overall: {verdict.OverallLevel}  (runs={verdict.RunCount}, spread={verdict.Spread:F3})");
        Console.WriteLine($"Summary: {verdict.Summary}");
        Console.WriteLine("\nPer-axis (mean level/score across runs):");
        foreach (var g in axisScores.GroupBy(a => a.Axis))
        {
            var modalLevel = g.GroupBy(x => x.Level).OrderByDescending(x => x.Count()).First().Key;
            Console.WriteLine($"  {g.Key,-20} {modalLevel,-14} mean={g.Average(x => x.Score):F2}");
        }
    }
    else
    {
        Console.WriteLine("(no verdict persisted)");
    }
    return;
}

// --- Autoeval (M4 go/no-go): `dotnet run -- eval [level,level,...]` runs synthetic profiles of known
// level and checks the verdict separates them (the core "can we trust the verdict?" hypothesis test).
if (args.Contains("eval"))
{
    var levelsArg = args.SkipWhile(a => a != "eval").Skip(1).FirstOrDefault() ?? "middle,senior";
    var levels = levelsArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var results = new List<(string Level, string Overall, double Mean, double Spread)>();
    foreach (var level in levels)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sim = sp.GetRequiredService<InterviewSimulator>();
        var db = sp.GetRequiredService<AppDbContext>();

        Console.WriteLine($"\n=== Eval: simulating '{level}' candidate ===");
        var sid = await sim.RunAsync($"eval-{level}", level);

        var verdict = await db.Verdicts.AsNoTracking().FirstOrDefaultAsync(v => v.SessionId == sid);
        var axes = await db.AxisScores.AsNoTracking().Where(a => a.SessionId == sid).ToListAsync();
        var mean = axes.Count == 0 ? 0 : axes.Average(a => a.Score);
        results.Add((level, verdict?.OverallLevel ?? "(none)", mean, verdict?.Spread ?? 0));
        Console.WriteLine($"  -> overall='{verdict?.OverallLevel}'  mean={mean:F2}  spread={verdict?.Spread:F3}");
    }

    Console.WriteLine("\n=== Eval summary ===");
    foreach (var r in results)
        Console.WriteLine($"  {r.Level,-10} -> {r.Overall,-18} mean={r.Mean:F2}  spread={r.Spread:F3}");

    var sen = results.FirstOrDefault(r => r.Level.Equals("senior", StringComparison.OrdinalIgnoreCase));
    var mid = results.FirstOrDefault(r => r.Level.Equals("middle", StringComparison.OrdinalIgnoreCase));
    if (sen.Level is not null && mid.Level is not null)
    {
        // A trustworthy verdict must do BOTH: rate the senior profile Senior AND rate the middle profile
        // below Senior. A mere numeric gap is not enough — if the middle profile is also classified
        // "Senior", the verdict over-rates (the LLM-flattery failure mode this product exists to avoid).
        var isSenior = (string lvl) => lvl.Contains("senior", StringComparison.OrdinalIgnoreCase)
                                       && !lvl.Contains("middle", StringComparison.OrdinalIgnoreCase);
        var seniorOk = isSenior(sen.Overall);
        var middleBelowSenior = !isSenior(mid.Overall);
        var pass = seniorOk && middleBelowSenior;
        Console.WriteLine(
            $"\nGO/NO-GO: senior='{sen.Overall}' (mean {sen.Mean:F2}) vs middle='{mid.Overall}' (mean {mid.Mean:F2}), "
            + $"Δ={sen.Mean - mid.Mean:+0.00;-0.00}");
        Console.WriteLine(pass
            ? "  PASS — senior rated Senior, middle rated below Senior."
            : "  NO-GO — verdict does not separate the levels (middle was NOT rated below Senior). "
              + "Likely cause: the simulated 'middle' answers are not genuinely middle-level (strong model "
              + "leaks competence through the persona). Needs stronger ground-truth fixtures before trusting calibration.");
    }
    return;
}

// --- Voice simulation: `dotnet run -- voice-demo [level]` drives the whole interview through the
// turn-oriented voice adapter (IVoiceInterview), with the simulated candidate standing in for STT output.
// Proves the single utterance-in/utterance-out interface a managed provider plugs into. M5 "go".
if (args.Contains("voice-demo"))
{
    var level = args.SkipWhile(a => a != "voice-demo").Skip(1).FirstOrDefault() ?? "senior";

    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var voice = sp.GetRequiredService<IVoiceInterview>();
    var candidate = sp.GetRequiredService<ICandidate>();
    var db = sp.GetRequiredService<AppDbContext>();

    var start = await voice.StartAsync($"voice-{level}");
    var sessionId = start.SessionId;
    var utterance = start.Utterance;
    Console.WriteLine($"=== Voice simulation (simulated {level} via turn adapter) — session {sessionId} ===");

    var complete = false;
    while (!complete)
    {
        var round = await db.Rounds.AsNoTracking()
            .Where(r => r.SessionId == sessionId && r.Status == "open")
            .OrderBy(r => r.Order).LastOrDefaultAsync();
        var nodeCtx = string.Empty;
        if (round?.PendingSkillId is { } sk)
        {
            var node = await db.SkillNodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sk);
            if (node is not null)
                nodeCtx = $"{node.Title}: {node.Description}\nSenior signal: {node.SeniorSignal}";
        }

        Console.WriteLine($"\n🔊 Interviewer: {utterance}");
        var answer = await candidate.AnswerAsync(utterance, nodeCtx, level);
        Console.WriteLine($"🎤 Candidate: {answer}");

        var turn = await voice.NextTurnAsync(sessionId, answer);
        utterance = turn.Utterance;
        complete = turn.IsComplete;
    }

    Console.WriteLine($"\n🔊 Interviewer (closing): {utterance}");
    var verdict = await db.Verdicts.AsNoTracking().FirstOrDefaultAsync(v => v.SessionId == sessionId);
    Console.WriteLine($"\n=== Verdict ===\n{(verdict is null ? "(none)" : $"{verdict.OverallLevel} (runs={verdict.RunCount}, spread={verdict.Spread:F3})")}");
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

// Serve the built React SPA (frontend/ -> wwwroot) same-origin: index at "/", static assets, and a
// fallback so client-side routes resolve to index.html. API routes below take precedence.
app.UseDefaultFiles();
app.UseStaticFiles();

// Access gate: when Security:InviteCode is set (public deploys), interview endpoints require a matching
// X-Invite-Code header so a public, LLM-backed app can't be drained by bots/strangers. Empty => open (dev).
// The SPA and /health stay open so the page loads and the code can be entered in the UI.
var inviteCode = app.Configuration["Security:InviteCode"];
if (!string.IsNullOrWhiteSpace(inviteCode))
{
    app.Use(async (ctx, next) =>
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        var gated = path.StartsWith("/voice", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/sessions", StringComparison.OrdinalIgnoreCase);
        if (gated && ctx.Request.Headers["X-Invite-Code"].ToString() != inviteCode)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "Invalid or missing invite code." });
            return;
        }
        await next();
    });
}

// --- Endpoints -----------------------------------------------------------

app.MapGet("/health", () => Results.Ok(new HealthResponse("ok")))
    .WithName("Health");

app.MapPost("/sessions", async (
        CreateSessionRequest body,
        IInterviewOrchestrator orchestrator,
        CancellationToken ct) =>
    {
        var result = await orchestrator.StartAsync(
            new StartInterviewRequest(body.CandidateRef, body.GraphVersion), ct);

        var dto = new SessionResponse(
            result.SessionId,
            result.State.ToString(),
            result.FirstQuestion);

        return Results.Created($"/sessions/{result.SessionId}", dto);
    })
    .WithName("CreateSession");

app.MapGet("/sessions/{id:guid}", async Task<Results<Ok<SessionDetailResponse>, NotFound>> (
        Guid id,
        AppDbContext db,
        CancellationToken ct) =>
    {
        var session = await db.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new SessionDetailResponse(
            session.Id,
            session.State.ToString(),
            session.Status.ToString(),
            session.StartedAt,
            session.GraphVersion,
            session.PromptVersion,
            session.ModelId));
    })
    .WithName("GetSession");

// Report dashboard: the persisted verdict + per-axis breakdown (averaged over scorer runs).
app.MapGet("/sessions/{id:guid}/verdict", async Task<Results<Ok<VerdictResponse>, NotFound>> (
        Guid id,
        AppDbContext db,
        CancellationToken ct) =>
    {
        var verdict = await db.Verdicts.AsNoTracking().FirstOrDefaultAsync(v => v.SessionId == id, ct);
        if (verdict is null)
        {
            return TypedResults.NotFound();
        }

        var axes = await db.AxisScores.AsNoTracking()
            .Where(a => a.SessionId == id)
            .ToListAsync(ct);

        var perAxis = axes
            .GroupBy(a => a.Axis)
            .Select(g => new AxisVerdictDto(
                g.Key.ToString(),
                g.GroupBy(x => x.Level).OrderByDescending(x => x.Count()).First().Key,
                Math.Round(g.Average(x => x.Score), 2),
                g.OrderByDescending(x => x.RunIndex).First().Rationale,
                g.SelectMany(x => x.Citations).Distinct().ToArray()))
            .ToArray();

        return TypedResults.Ok(new VerdictResponse(
            verdict.SessionId, verdict.OverallLevel, verdict.Summary, verdict.RunCount, verdict.Spread, perAxis));
    })
    .WithName("GetVerdict");

// --- Voice endpoints: a managed provider (Retell/Vapi/OpenAI Realtime) drives these. STT/TTS/turn-taking
// is the provider's job; the engine only returns the next utterance. See docs/voice-integration.md.
app.MapPost("/voice/sessions", async (
        VoiceStartBody body,
        IVoiceInterview voice,
        CancellationToken ct) =>
    {
        var r = await voice.StartAsync(body.CandidateRef, body.Language, ct);
        return Results.Created($"/voice/sessions/{r.SessionId}",
            new VoiceTurnResponse(r.SessionId, r.Utterance, IsComplete: false));
    })
    .WithName("VoiceStart");

app.MapPost("/voice/sessions/{id:guid}/turn", async (
        Guid id,
        VoiceTurnBody body,
        IVoiceInterview voice,
        CancellationToken ct) =>
    {
        var r = await voice.NextTurnAsync(id, body.Utterance, ct);
        return Results.Ok(new VoiceTurnResponse(r.SessionId, r.Utterance, r.IsComplete));
    })
    .WithName("VoiceTurn");

// Speech-to-text: the browser POSTs recorded audio (raw body), we return the transcript.
app.MapPost("/voice/stt", async (HttpRequest request, SpeechService speech, CancellationToken ct) =>
    {
        if (!speech.IsConfigured)
            return Results.Problem("Voice is not configured (Voice:ApiKey missing).", statusCode: 503);
        var text = await speech.TranscribeAsync(request.Body, request.ContentType, ct);
        return Results.Ok(new SttResponse(text));
    })
    .WithName("VoiceStt");

// Text-to-speech: returns MP3 audio for the given text (the interviewer's utterance).
app.MapPost("/voice/tts", async (VoiceTtsBody body, SpeechService speech, CancellationToken ct) =>
    {
        if (!speech.IsConfigured)
            return Results.Problem("Voice is not configured (Voice:ApiKey missing).", statusCode: 503);
        var audio = await speech.SynthesizeAsync(body.Text, ct);
        return Results.File(audio, "audio/mpeg");
    })
    .WithName("VoiceTts");

app.MapPost("/sessions/{id:guid}/answer", async (
        Guid id,
        SubmitAnswerBody body,
        IInterviewOrchestrator orchestrator,
        CancellationToken ct) =>
    {
        var result = await orchestrator.SubmitAnswerAsync(
            new SubmitAnswerRequest(id, body.Answer), ct);

        return Results.Ok(new AnswerResponse(
            result.SessionId,
            result.State.ToString(),
            result.NextQuestion,
            result.RoundComplete));
    })
    .WithName("SubmitAnswer");

// SPA fallback: any non-API, non-file path returns index.html (client-side routing).
app.MapFallbackToFile("index.html");

// Self-initialize the database on server startup so a fresh deployment is self-contained:
// apply any pending migrations, then seed the skill graph if the table is empty. Idempotent.
// (CLI modes above return before this point, so it runs only when serving.)
using (var initScope = app.Services.CreateScope())
{
    var sp = initScope.ServiceProvider;
    await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    if (!await sp.GetRequiredService<AppDbContext>().SkillNodes.AnyAsync())
    {
        var graphPath = Path.GetFullPath(app.Configuration["Content:GraphPath"] ?? "content/skill-graph.json");
        var count = await sp.GetRequiredService<GraphSeeder>().SeedFromFileAsync(graphPath);
        app.Logger.LogInformation("Seeded {Count} skill nodes on startup from {Path}.", count, graphPath);
    }
}

app.Run();
