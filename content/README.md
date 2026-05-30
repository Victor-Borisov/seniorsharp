# content/ — the assessment content

This directory is the **data layer** of SeniorSharp: the opinion about *what* makes a Senior .NET engineer.
The code under `engine/` is generic interview-orchestration machinery — it knows *how* to run an assessment
but has no opinion baked in. That opinion lives here, as data, and is fully open: swap these files and you
re-target the whole product (a different stack, a different seniority bar) without touching engine code.

## contents

- **`skill-graph.json`** — the competency graph that drives questioning: 91 senior-distinguishing nodes
  across layers (C#, runtime/CLR, ASP.NET Core, EF Core, concurrency, cross-cutting, architecture). Each node
  carries `seniorSignal` (what separates a senior answer from a middle one) and an `exampleProbe`. The graph
  is grounded against public catalogs (roadmap.sh, Microsoft Learn) plus senior-depth nodes on top.
  Schema: `{ version, date, nodes: [{ id, title, layer, description, axes[], prerequisites[],
  mastery_focus[], senior_signal, example_probe, provenance? }] }`. Loaded by `GraphSeeder`.

- **`criteria.md`** — the scoring criteria: four axes (TechnicalDepth, Architecture, ProductionMaturity,
  Communication), each with level descriptors (middle / strong-middle / senior / expert), observable signals,
  and the explicit strong-middle→senior boundary plus anti-flattery disqualifiers. Fed to the scorer.

- **`prompts/`** — the system prompts for each role:
  - `questioner.v1.md` — picks the next skill node and phrases the probe.
  - `classifier.v1.md` — grades one answer (recognition / application / depth).
  - `scorer.v1.md` — produces the final per-axis verdict from the transcript.
  - `discovery.v1.md` — opens the interview with an experience/ownership round.
  - `candidate-sim.v1.md` — simulated candidate for the eval harness (not a production role).

## versioning

Prompt files are versioned in their filename (`*.v1.md`). The active version is recorded per session via
`PromptVersion`, so historical verdicts stay reproducible and auditable. Bump the suffix (`.v2.md`) rather
than editing a shipped prompt in place.

## tuning the bar

To adjust what "Senior" means for your team, edit `criteria.md` (the bar) and the node `seniorSignal`s (what
each topic tests). The prompts reference these as data — no code change needed.
