# SeniorSharp

SeniorSharp is an AI interviewer that assesses whether a developer is Senior .NET. It conducts an adaptive,
multi-round interview and produces an explained, cited verdict across four axes: Technical Depth,
Architecture, Production Maturity, and Communication.

## Assessment content

The assessment content is prepared ahead of time and committed as versioned data under `content/`; it is not
generated per session. Two artefacts define the bar:

- **Skill graph** (`content/skill-graph.json`) - the topics worth probing. It is grounded on recognized
  public catalogs (the [roadmap.sh](https://roadmap.sh) C# and ASP.NET Core roadmaps and the Microsoft Learn
  .NET paths, cross-checked against the AZ-204 outline) and extended with senior-depth nodes the catalogs do
  not cover. Each node records its catalog provenance. Explore it interactively in the
  [**skill graph explorer**](https://seniorsharp.net/skill-graph.html) - hover a node to trace its
  prerequisites and what it unlocks, click for the senior signal and an example probe question, and filter by
  evaluation axis.
- **Criteria** (`content/criteria.md`) - per-axis level descriptors, including where the line between
  *senior* and *not senior* sits. They were calibrated by Claude (Opus-class) through an adversarial,
  anti-flattery review.

At runtime the engine drives the interview against this fixed graph and criteria: the model conducts the
dialogue and scores the answers against the committed bar, with no human-labelled training data or recorded
interviews. Because the bar lives in versioned content, verdicts are reproducible and auditable, and the
definition of "Senior" is changed by editing content rather than code.

The engine (`engine/`) is generic interview machinery and carries no domain opinion; the content (`content/`)
carries it. Retargeting to a different stack or a different seniority bar is a content change.

## How it works

An interview is a deterministic finite-state machine, not an autonomous agent:

```
Discovery → DeepDive → SystemDesign → Scoring → Report → Done
```

- **Discovery** - a short conversational round on real experience, ownership and red flags.
- **DeepDive** - adaptive, graph-driven technical probing. A questioner selects the next skill node; the
  candidate answers; a classifier grades the answer on recognition / application / depth and updates the
  per-skill mastery, which informs the next selection.
- **SystemDesign** - the same adaptive loop over the architecture layer of the graph.
- **Scoring** - a scorer runs several times over the full transcript (an ensemble), producing per-axis
  scores that are aggregated into a verdict; the variance across runs (spread) is reported as a confidence
  signal.
- **Report** - the persisted verdict: overall level and per-axis level, score, rationale and transcript
  citations.

## Design notes

- **Structured output via forced tool-use.** Every decision step (question selection, classification,
  scoring) is a forced tool call, validated against a JSON schema and retried on mismatch.
- **Prompt caching.** The stable prefix (role prompt, skill subgraph, criteria) is cached across a session.
- **Ensemble and spread.** Multiple scorer runs measure consistency; their spread accompanies the verdict.
- **Provider abstraction.** A thin `ILlmClient` keeps the engine model-agnostic, with a per-role model override.
- **Eval harness.** Synthetic candidates of a known level are run end to end to confirm the verdict separates
  levels - a middle profile must score below senior - which guards against the tendency of LLMs toward leniency.
- **Voice as an I/O layer.** A turn-oriented adapter (`IVoiceInterview`) lets a managed voice provider drive
  the same engine. The verdict is always computed from the transcript, so text and voice share one core.

## Tech stack

- **Backend:** ASP.NET Core (.NET 10), EF Core + PostgreSQL, the official Anthropic SDK (Claude).
- **Frontend:** React + Vite (TypeScript), built into the API's `wwwroot` and served same-origin.
- **Voice (I/O):** OpenAI STT/TTS (`gpt-4o-transcribe` / `gpt-4o-mini-tts`); browser voice mode (record answers, hear questions) wired into the UI.
- **Observability:** OpenTelemetry exported to Langfuse.

## Repository layout

```
SeniorSharp.sln
engine/
  SeniorSharp.Domain          entities + enums
  SeniorSharp.Contracts       prompt DTOs + JSON schemas
  SeniorSharp.Persistence     EF Core (PostgreSQL), graph seeder
  SeniorSharp.Llm             ILlmClient + Anthropic adapter (forced tool-use, prompt caching)
  SeniorSharp.Orchestration   FSM, questioner/classifier/scorer, voice adapter, eval simulator
  SeniorSharp.Api             minimal API + serves the SPA
  SeniorSharp.Tests           xUnit (SQLite in-memory)
frontend/                     React SPA (Vite + TS)
content/                      skill graph (grounded on public catalogs), criteria, role prompts
Dockerfile                    multi-stage: build SPA + publish API into one image
docker-compose.yml            app + postgres + langfuse
```

## Running locally

Prerequisites: .NET 10 SDK, Docker, Node 20, an `ANTHROPIC_API_KEY`.

```bash
# 1. Start Postgres
docker compose up -d postgres

# 2. Apply the schema and seed the skill graph
dotnet tool install --global dotnet-ef        # once
export ANTHROPIC_API_KEY=sk-ant-...
dotnet ef database update \
  --project engine/SeniorSharp.Persistence --startup-project engine/SeniorSharp.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --project engine/SeniorSharp.Api -- seed

# 3. Build the web UI into wwwroot
cd frontend && npm install && npm run build && cd ..

# 4. Run the API (serves the SPA at http://localhost:5000)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project engine/SeniorSharp.Api
```

### CLI modes (`dotnet run --project engine/SeniorSharp.Api -- <mode>`)

| Mode | What it does |
|------|--------------|
| `seed` | upsert the skill graph into the database |
| `score-demo` | score a fixture transcript and print the verdict |
| `deepdive-demo [level]` | run the deep-dive loop with a simulated candidate |
| `interview-demo [level]` | run the full interview (all rounds) and print the verdict |
| `eval middle,senior` | run synthetic profiles and check that the verdict separates levels |
| `voice-demo [level]` | drive the whole interview through the voice turn adapter |

## Configuration (environment variables)

| Variable | Purpose |
|----------|---------|
| `ANTHROPIC_API_KEY` | Claude API key (interviewer / scorer) |
| `Anthropic__Model` | model id (default `claude-opus-4-8`) |
| `ConnectionStrings__Postgres` | PostgreSQL connection string |
| `Voice__ApiKey` | OpenAI key for voice STT/TTS |
| `Security__InviteCode` | access gate for public deployments (empty disables the gate) |
| `Interview__DiscoveryBudget` / `DeepDiveBudget` / `SystemDesignBudget` / `ScorerRuns` | round and scoring budgets |

The dominant running cost is LLM usage (roughly $0.5–2 per interview), not hosting. For a public deployment,
set an invite code and modest budgets.

## Deployment

The Dockerfile builds the SPA and the API into a single image, and the backend serves the SPA same-origin
(one URL, no CORS). It runs on any container host (for example Coolify on a small VPS) with a managed or
compose-provided PostgreSQL and the environment variables above; TLS is handled by the platform.

## Status

The engine is complete end to end: a full interview produces a persisted, explained, level-separating
verdict, served through a web UI with both text and browser voice (record answers, hear questions via
OpenAI STT/TTS).
