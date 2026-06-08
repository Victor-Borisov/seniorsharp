# Senior .NET Assessment Criteria (draft v0)

> **Status:** draft v0, model-generated 2026-05-28 (generation → adversarial anti-flattery critic). **Requires your review and bar calibration** (§1.4, §2 product-spec). 
> Assessment along 4 axes, each with level descriptors middle / strong middle / **senior** / expert. Verdict — a profile across axes + justification with quotes (§5 product-spec).

## Axes

| Axis | What we assess |
|---|---|
| **Technical depth** | Depth of understanding of .NET platform internals: CLR, GC, the memory model, async/await as a state machine, EF Core (tracking, expression translation, materialization), performance (allocations, Span/Memory, benchmark correctness), concurrency. We distinguish those who KNOW HOW TO USE an API from those who explain the MECHANISM under the hood and ANTICIPATE edge cases BEFORE they manifest, without waiting for leading questions. CRITICAL BOUNDARY strong-middle->senior: a senior, UNPROMPTED, without any hint, brings up internals and failure modes (ValueTask double-await, GC mode for the load profile, the memory model, EF translation limits) and connects them to observed behavior (p99, allocations, deadlock); a strong middle names the same terms, but only reactively, and does not get down to the mechanism or the cost. |
| **Architecture** | Ability to design systems: decomposition, service/module boundaries, choice of integration style (sync/async, messaging), data and consistency management, explicit work with trade-offs and constraints. We distinguish 'knows patterns by name' from 'derives the solution from the requirements and HIMSELF names the cost of and the conditions for abandoning each alternative'. CRITICAL BOUNDARY strong-middle->senior: a senior STARTS by clarifying NFRs/constraints and HIMSELF, without being asked 'and what are the downsides?', names the cost of his solution and the conditions under which he would choose a different one, and himself brings up distributed-systems problems (outbox/saga, idempotency, deduplication); a strong middle names trade-offs only when asked, and usually a single pair. |
| **Production maturity** | Operational maturity: debugging real problems, observability (logs/metrics/traces), behavior during incidents, diagnosis from SYMPTOMS without a debugger, resilience and degradation, releases. We distinguish 'I can write code' from 'I can keep a system alive in production and quickly find the root of a problem'. CRITICAL BOUNDARY strong-middle->senior: a senior, given a set of symptoms (latency/CPU/memory/GC), HIMSELF puts forward a specific .NET-specific hypothesis and names a SPECIFIC tool/confirmation step (dump -> heap/thread analysis, trace -> hot path, counters -> threadpool/GC) and designs observability UP FRONT; a strong middle names tools by name, but lacks a confident workflow and gets stuck at the symptom. |
| **Communication / process** | How the candidate reasons aloud, justifies decisions and trade-offs, leads a technical discussion, does code review, and develops others. We distinguish 'produces an answer' from 'structures their thinking, makes assumptions explicit, separates facts from hypotheses, acknowledges uncertainty with a verification plan, and raises the team's level'. CRITICAL BOUNDARY strong-middle->senior: a senior HIMSELF structures the answer (assumptions -> options -> trade-offs -> recommendation), explicitly separates 'I know / I assume / needs to be verified in such-and-such a way', and when he doesn't know, gives a SPECIFIC verification plan (experiment/benchmark/prototype); a strong middle reasons coherently and admits 'not sure', but does not always formulate a verification plan and does not take the trade-off to a conclusion. |

---

## Technical depth

*Depth of understanding of .NET platform internals: CLR, GC, the memory model, async/await as a state machine, EF Core (tracking, expression translation, materialization), performance (allocations, Span/Memory, benchmark correctness), concurrency. We distinguish those who CAN USE THE API from those who explain the MECHANISM under the hood and ANTICIPATE edge cases BEFORE they manifest, without waiting for leading questions. CRITICAL BOUNDARY strong-middle->senior: a senior, on his own and without prompting, brings up internals and failure modes (ValueTask double-await, GC mode for the load profile, the memory model, EF translation limits) and connects them to observable behavior (p99, allocations, deadlock); a strong middle names the same terms, but only reactively, and does not follow through to the mechanism or to the cost.*

### middle

Writes working code with async/await, LINQ, EF, but explains at the level of 'await waits for completion', 'GC cleans up memory', 'struct is faster than class — it lives on the stack'. Confuses asynchrony with parallelism. About GC generations knows only 'gen0 more often', with no hypothesis about object lifetime. Does not see hidden allocations (closures, boxing, the async state machine). Catches N+1 and EF tracking problems after the fact from a slow query, not from the code.

**Observable signals:**
- When asked 'what does await do', answers in terms of blocking/waiting, not about returning the thread to the pool and resuming via a continuation
- Explains the division of GC into generations as 'gen0 is collected more often' without the reason (does not state the object lifetime hypothesis)
- Does not mention SynchronizationContext/ConfigureAwait at all, or says 'so there are no deadlocks' without identifying the mechanism
- Reduces struct vs class to 'stack vs heap'; does not mention copy-by-value, defensive copy, readonly struct
- Talks about N+1 only after being shown a slow query or a metric; does not predict it from the code

### strong middle

Understands that await compiles into a state machine, releases the thread back to the pool, and the continuation is scheduled on the captured context; knows ConfigureAwait(false) and where it matters. Explains GC generations through the object lifetime hypothesis; knows about the LOH and the 85K threshold at the level of 'fragmentation can happen'. Sees some of the hidden allocations. In EF deliberately applies AsNoTracking, Include, projections to DTOs, distinguishes client/server evaluation. BOUNDARY: even when the topic of GC/ValueTask/concurrency is raised for him, does NOT follow through to the mechanism and does not arrive at the trade-off for a specific load; ValueTask, pooling, GC modes, contention on concurrent collections — names them, but flounders when probed deeper.

**Observable signals:**
- Describes async as a state machine with MoveNext, but on a follow-up question about ExecutionContext/context capture gets confused or answers in generalities
- Applies AsNoTracking, split query, projections; explains when tracking is unnecessary — but does not unpack expression translation to SQL or provider limits
- Names the LOH and the 85K threshold reactively, but does not discuss compaction/GC modes/configuration even under a leading question
- Recognizes boxing and closure allocation when they are pointed out, but does not flag them in his own code himself (async method, LINQ iterators)
- Explains the deadlock mechanism from .Result/.Wait via context capture correctly, but without generalizing to other starvation scenarios

### senior

WITHOUT leading questions brings up internals and failure modes and connects them to observable behavior. Async: state machine, ExecutionContext flow, ConfigureAwait in a library vs an application, ValueTask and its specific pitfalls (awaiting twice, reading before completion, cannot be cached). GC: server vs workstation, concurrent/background, impact on tail latency, diagnostics via counters/traces, LOH fragmentation and fighting it. Concurrency: the memory model, volatile/Interlocked/lock, torn reads, false sharing, the cost of lock-free. EF: expression translation to SQL, provider limits, the change tracker, batching, and a justified point at which to drop down to Dapper/raw SQL. DISQUALIFICATION from senior: proposes optimizations (Span/stackalloc/pooling) without the caveat 'if the profiler showed it'; calls ValueTask 'just faster than Task' without semantics; does not connect the GC choice to the load profile.

**Observable signals:**
- UNPROMPTED, without a hint, articulates a specific pitfall: 'ValueTask must not be awaited twice and .Result must not be read before completion' — not just 'ValueTask saves an allocation'
- Connects the GC mode/configuration to a measurable goal: 'latency-sensitive -> server GC + concurrent, otherwise pauses hit p99', and names how to confirm it (dotnet-counters, GC pause %)
- Explains false sharing/torn read via the memory model and cache lines, not by the heuristic 'add volatile just in case'
- Points out hidden allocations in his own code (async state machine, closure, LINQ, boxing) and proposes a fix ONLY conditional on profiler measurement
- About EF names a specific limit: 'the provider will not translate this expression -> it will go to client evaluation or fail; here I will unroll it into raw SQL', stating how he verified the plan/SQL

### expert

Reasons at the level of the runtime's design and its evolution, sees system-wide effects across a fleet of services. Tiered JIT, dynamic PGO, inlining/devirtualization and their impact on warmup and microbenchmark correctness. Write barriers, card table, ephemeral GC, pinning. ThreadPool starvation as a systemic failure via hill-climbing. Operates with quantitative budgets (allocations/ms, p99/p999 targets) and derives engineering constraints from them; sets team standards and tooling.

**Observable signals:**
- Mentions tiered compilation/PGO and specifically how it breaks a microbenchmark (no warmup -> you measure Tier0) and how to fix it (warmup, [Benchmark] with iterations)
- Explains ThreadPool starvation via hill-climbing and blocking calls, gives diagnostics (ThreadPool queue length counter) and a systemic cure, not 'add more threads'
- Connects write barriers/card table/pinning to a specific perf incident (e.g. pinned buffers -> Gen2 fragmentation -> growing pauses)
- Derives an engineering constraint from a quantitative budget: 'a budget of 200 KB/req at 5k rps -> 1 GB/s of allocations -> X Gen0 collections/s -> such-and-such perf gate in CI'
- Proposes concrete team artifacts: an async guide, a perf gate in CI with a threshold, a BenchmarkDotNet project template
## Architecture

*The ability to design systems: decomposition, service/module boundaries, choice of integration style (sync/async, messaging), data and consistency management, explicit work with trade-offs and constraints. We distinguish 'knows patterns by name' from 'derives the solution from requirements and UNPROMPTED names the cost of and conditions for rejecting each alternative'. CRITICAL BOUNDARY strong-middle->senior: a senior STARTS by clarifying NFR/constraints and ON HIS OWN, without being asked 'and what are the downsides?', names the cost of his solution and the conditions under which he would choose a different one, and raises distributed problems himself (outbox/saga, idempotency, deduplication); a strong middle names trade-offs only when asked, and usually as a single pair.*

### middle

Designs within a single application using familiar templates (layers, repository, DI). Justification — 'that's the convention'/'that's how it is in clean architecture'. Applies microservices/CQRS/event sourcing as cargo cult. Cannot articulate the downsides of his solution or speaks in generalities ('it will get more complex'). Does not raise transactional boundaries and consistency between services himself. Draws boundaries along technical layers.

**Observable signals:**
- Justification for a decision = 'it's a best practice'/'that's how the book says', without tying it to requirements
- Proposes microservices/CQRS without a concrete problem they solve
- When directly asked 'what are the downsides of your solution?', struggles to answer or gives a generic 'it's more complex'
- Does not bring up the topic of transactions/consistency between services even when the design is distributed
- Module boundaries — by layers (controllers/services/repos), not by domains

### strong middle

Confidently designs a medium-sized service, draws boundaries along domains, distinguishes sync/async integration and basic messaging patterns. Knows idempotency, retry, eventual consistency. BOUNDARY: names trade-offs ONLY on request and usually as a single pair (consistency/availability); does not start with NFR himself; raises distributed problems (saga, deduplication with at-least-once) reactively; does not get to a quantitative cost or to the reversibility of a decision.

**Observable signals:**
- Draws service boundaries along business capabilities and explains why — but does not appeal to NFR (load/latency/team) himself
- Distinguishes orchestration/choreography, REST/queues; names when to use which — after a follow-up question
- Formulates a trade-off as a pair only when asked 'and the downsides?'; does not lay it out himself
- Mentions idempotency and retry, but does not spell out the link to at-least-once and the need for deduplication without a hint
- Discusses versioning/contract evolution reactively, as a problem, not as part of the design

### senior

STARTS by clarifying NFR and constraints (load, latency, consistency, team, deadlines) BEFORE proposing a solution, and UNPROMPTED voices the cost of each option and why he rejects the alternatives. Without a hint, raises distributed problems: outbox/saga, idempotency, at-least-once + deduplication, consistency via events, backward compatibility of contracts. Explicitly separates reversible and irreversible decisions. Consciously chooses monolith/modular monolith/services for the context, accounts for Conway's law, designs for evolution (versioning, migrations, degradation). DISQUALIFICATION from senior: proposed an architecture before clarifying at least one NFR; did not name a SINGLE concrete downside of his choice on his own initiative; 'microservices' without discussing distributed transactions.

**Observable signals:**
- His very first reply clarifies NFR/constraints ('what is the load, what consistency lag is acceptable, what is the team size?') before naming a solution
- For EVERY decision of his, names a concrete downside and a switching condition himself: 'I chose X; the cost is Y; if the load grows to Z, I will switch to W'
- On his own, without being asked, raises outbox/saga + idempotency + deduplication when describing inter-service interaction
- Explicitly marks a decision as reversible/irreversible ('this is a two-way door, it can be rolled back' vs 'the database choice here is irreversible')
- Ties boundaries to team ownership and discusses a contract evolution plan (versioning, backward compatibility for N releases)

### expert

Thinks at the level of a platform and a portfolio of systems. Formulates architectural principles that scale across many teams; decides what to standardize and what to leave autonomous. Performs explicit risk analysis and migration cost analysis, a strategy of phased evolution (strangler, contract tests, dual-write). Evaluates decisions in money/time and in impact on the velocity of multiple teams. Deliberately does NOT add complexity, arguing for right-sizing.

**Observable signals:**
- Reasons about a set of services/a platform: which decisions should be shared standards, and which — left to the teams' discretion
- Provides a concrete strategy for phased migration (strangler fig, dual-write + reconciliation, contract tests) with stages and rollback points
- Evaluates a decision in cost/risk and in impact on the throughput of several teams, not just one
- Ties architecture to org design and product strategy, not just to technology
- Argues for the simple solution with reasons: 'the added complexity of X does not pay off at the current scale Y, we will revisit at Z'
## Production maturity

*Operational maturity: debugging real problems, observability (logs/metrics/traces), behavior during incidents, diagnosing from SYMPTOMS without a debugger, resilience and degradation, releases. We distinguish 'can write code' from 'can keep the system alive in production and quickly find the root of the problem'. CRITICAL BOUNDARY strong-middle->senior: a senior, given a set of symptoms (latency/CPU/memory/GC), UNPROMPTED puts forward a concrete .NET-specific hypothesis and names a CONCRETE tool/confirmation step (dump -> heap/thread analysis, trace -> hot path, counters -> threadpool/GC) and designs observability IN ADVANCE; a strong-middle names the tools by name, but has no confident workflow and gets stuck at the symptom.*

### middle

Debugs locally with a debugger and Console/ILogger. In production looks at errors in logs without structure or correlation. Metrics/traces are 'something DevOps sets up'. For a memory leak/hang in production suggests 'restart it' or 'add more logging'. Cannot tell a symptom from a cause.

**Observable signals:**
- Diagnostics = 'look at the logs' and 'reproduce locally', there is no other plan
- For a memory leak/high CPU in production has no concrete tool (does not name dump/profiler/counters)
- Logs without correlation id, without meaningful levels, without structured fields
- Calls metrics/traces someone else's area of responsibility
- Responds to an incident with a restart, with no hypothesis about the cause

### strong middle

Writes structured logs with levels and a correlation id, uses metrics and alerts, understands basic distributed tracing. Knows dotnet-counters/dotnet-trace/dotnet-dump BY NAME. In an incident starts from dashboards, narrows down the area. BOUNDARY: for a non-trivial problem (intermittent degradation, ThreadPool starvation, GC pauses, a deadlock in production) does not build a hypothesis on his own and does not get to root cause; named the tool, but has no coherent dump/trace analysis workflow; gets stuck at the symptom.

**Observable signals:**
- Describes structured logging, correlation id, meaningful log levels — does this confidently
- Names dotnet-dump/dotnet-counters/dotnet-trace, but when asked 'what next with the dump?' has no concrete steps (clrstack/dumpheap/threads)
- In an incident starts from dashboards and narrows down the area, but stops at a non-trivial symptom without a hypothesis about the .NET cause
- Knows SLO/healthchecks/graceful shutdown at a basic level, without error budget
- Formulates an RCA after the fact, but does not get to the systemic cause (rather than the nearest trigger)

### senior

Diagnoses production from symptoms without a debugger: from the pattern of latency/CPU/memory/GC, UNPROMPTED puts forward a .NET-specific hypothesis (ThreadPool starvation, GC pauses, lock contention, connection pool exhaustion, a leak via statics/events/uncompleted Tasks) and names a CONCRETE way to confirm it. Designs observability in advance: what to log, SLIs/metrics, trace spans, alerts on symptoms without noise. In an incident: stabilization -> hypothesis -> confirmation -> fix -> blameless postmortem with a SYSTEMIC conclusion. Builds in resilience: timeouts, retry with backoff+jitter, circuit breaker, bulkhead, graceful degradation, idempotency. DISQUALIFICATION from senior: responds to a symptom with 'I'd take a dump/profiler' without a hypothesis of WHAT he is looking for; alerts 'on every error'; does not bring up resilience (timeout/retry/breaker) himself in design.

**Observable signals:**
- For the symptom 'latency is growing, pool threads are running out, CPU is low' ON HIS OWN names 'ThreadPool starvation from sync-over-async' and the confirmation step (ThreadPool queue counter, a dump with thread stacks)
- Describes a concrete workflow with commands/steps: dump -> dumpheap for top objects / clrstack across threads; trace -> hot path; counters -> GC pause % and threadpool
- Sets alerts on symptoms (p99 growth, error rate, pool saturation), explicitly against 'an alert on every error'; talks about SLI/SLO and error budget
- When designing a service, himself builds in timeout + retry with jitter + circuit breaker + bulkhead and explains which failure mode each one addresses
- Ends a postmortem with a SYSTEMIC measure ('we'll add a perf gate/change the pool default'), not a personal one ('we'll be more careful')

### expert

Raises the operational maturity of the entire fleet: observability standards, golden signals, unified dashboards and playbooks, a culture of error budget and postmortems. Designs diagnosability as a requirement (continuous profiling, distributed tracing with propagation, control of cardinality and telemetry cost). Reliability at the organization level: chaos/load testing, deployment strategies (canary, progressive), readiness for dependency failures. Turns major incidents into systemic changes to the platform and processes.

**Observable signals:**
- Describes organizational observability standards and an on-call/playbook culture for many teams
- Brings up continuous profiling and EXPLICITLY the control of cardinality/telemetry cost as an engineering constraint
- Links the deployment strategy (canary/progressive with auto-rollback by SLI) to release risk management quantitatively
- Uses error budget as a decision-making tool (feature freeze when exhausted), not as a metric on a dashboard
- From an incident derives a platform/process change that eliminates a CLASS of problems, not a single bug
## Communication / process

*How the candidate reasons out loud, justifies decisions and compromises, conducts a technical discussion, does code review, and develops others. We distinguish 'produces an answer' from 'structures thinking, makes assumptions explicit, separates facts from hypotheses, acknowledges uncertainty with a verification plan, and raises the team's level'. CRITICAL BOUNDARY strong-middle->senior: a senior structures the answer UNPROMPTED (assumptions -> options -> trade-offs -> recommendation), explicitly separates 'I know / I assume / this needs to be verified in such-and-such a way', and when he doesn't know, gives a CONCRETE verification plan (experiment/benchmark/prototype); a strong-middle reasons coherently and admits 'not sure', but doesn't always formulate a verification plan and doesn't carry the trade-off through to a conclusion.*

### middle

Answers on point, but reasoning is weakly structured: jumps around, doesn't state assumptions, on an open-ended question waits for a detailed spec instead of making explicit assumptions. Presents a decision as fact without alternatives. Code review = style and naming. Under disagreement, immediately gives in or digs in without arguments. Masks not knowing with a guess.

**Observable signals:**
- On an open-ended question doesn't make assumptions himself, demands a detailed spec
- Justification without alternatives and without 'why not otherwise'
- Review comments about formatting/naming, not about design and risks
- In a dispute quickly changes position or defends without arguments
- Passes a guess off as fact, doesn't flag uncertainty

### strong middle

Reasons coherently, talks through the line of thought and some of the assumptions. Gives an alternative when asked. In review catches bugs and some design problems, phrases comments constructively. Acknowledges the limits of his knowledge. BOUNDARY: doesn't maintain the structure 'assumptions -> options -> trade-offs -> conclusion' on his own without being asked; having said 'not sure', doesn't always formulate a verification plan; in a discussion hears the counterargument but doesn't carry the trade-off through to a shared conclusion; mentors reactively.

**Observable signals:**
- Talks through the reasoning steps and the main assumptions — but doesn't structure them explicitly on his own, without a request for structure
- Gives an alternative on request, compares along 1-2 axes, but doesn't always formulate a recommendation with justification
- In review finds logical/concurrency bugs and explains why it matters
- Says 'not sure', but doesn't always give a concrete verification plan (exactly how I'll verify it)
- In a dispute is respectful, hears the argument, but doesn't converge to a shared decision/conclusion

### senior

UNPROMPTED, without being asked, structures the answer: pins down the problem, makes explicit assumptions, breaks it into parts, leads toward a recommendation. Proposes alternatives on his own and honestly names trade-offs, EXPLICITLY separates facts/opinions/unknowns, and for the unknown gives a CONCRETE plan to remove the uncertainty (which experiment/benchmark/prototype and what it measures). In review looks at design, correctness, edge cases, testability, long-term maintenance — with justification and a proposed path. Changes his mind under facts explicitly. Mentors proactively through 'why'. DISQUALIFICATION from senior: no verification plan for 'I don't know'; names trade-offs only on request; in review doesn't go beyond bugs and style to design/maintainability.

**Observable signals:**
- On his own, without being asked, builds the answer as 'assumptions -> options -> trade-offs -> recommendation' and finishes with an explicit recommendation
- Explicitly flags: 'this I know for sure / this is my hypothesis / this needs to be verified — I'll run benchmark X and look at metric Y'
- Review comments about design/edge cases/testability/long-term maintenance, with justification and a proposed path, not just about bugs
- Changes position given a new fact and SAYS SO ('agreed, with a raw count like that my argument doesn't hold')
- Explains 'why', not just 'how'; the answer shows knowledge transfer to the interlocutor/team

### expert

Conveys complex things tailored to the audience: equally clear with engineers, product, and leadership, translates the technical side into risks/cost/timelines. Builds consensus and makes decisions under uncertainty and disagreement, recording the rationale (ADR/RFC) for scale. Raises engineering culture: standards for review and tech design, growing people through delegation and feedback, influence without formal authority. Manages the organization's technical narrative.

**Observable signals:**
- Adapts the explanation to the audience and translates the technical side into business impact (risk/money/timelines) with a concrete example
- Describes a practice of written decisions (ADR/RFC) and HOW he builds consensus under disagreement, not just what he documents
- Sets review/tech-design standards for teams (checklists, gates), not just participates in reviews
- Describes influencing a decision without formal authority through argument/data/trust, with an example
- Develops people through delegating significant tasks and structured feedback, not one-off tips
---

## Critic's notes

```
Tightenings relative to the draft (anti-flattery):

1. On EVERY axis, an explicit "CRITICAL strong-middle->senior BOUNDARY" has been added to what_we_assess — a single observable pivot rather than a gradient of adjectives. The pivot is the same everywhere: a senior does X ON THEIR OWN, without a leading question, and carries it through to the mechanism/cost/plan; a strong-middle does the same reactively and does not carry it through.

2. The strong-middle descriptors now include an explicit BOUNDARY: marker indicating exactly WHAT is missing to reach senior — so that the scorer does not inflate scores based on a mere match of terms.

3. The senior descriptors now include a DISQUALIFICATION: — specific behaviors that drop the candidate back to strong-middle even if they know the terms (Span without 'if the profiler showed it'; architecture before clarifying NFRs; 'I would take a dump' without a hypothesis of what they are looking for; 'I don't know' without a verification plan).

4. observable_signals have been rewritten from 'knows/understands X' (unverifiable) into transcript-verifiable formulations: what the candidate CONCRETELY says, in what order, with what tool/number. Many senior signals include a sample phrase ('ValueTask cannot be awaited twice', 'this is a door that swings both ways', 'agreed, with that raw count my argument does not hold'), which distinguishes understanding the mechanism from reciting the name.

5. Senior signals now require a LINK between internal knowledge and an observable effect (GC mode -> p99; starvation -> ThreadPool queue counter; allocation budget -> Gen0 collections/s), rather than an isolated fact.

Expert is kept as the upper bar (systemic/organizational thinking); it is not required for a Senior verdict. Domain-specific assessment criteria, no codebase access was required.
```
