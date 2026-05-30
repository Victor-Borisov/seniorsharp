using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SeniorSharp.Domain;
using SeniorSharp.Orchestration;
using Xunit;

namespace SeniorSharp.Tests;

public sealed class VoiceInterviewTests
{
    // Scripts orchestrator responses so we can assert how the voice adapter maps them to utterances.
    private sealed class ScriptedOrchestrator : IInterviewOrchestrator
    {
        private readonly Queue<object> _steps;
        public ScriptedOrchestrator(params object[] steps) => _steps = new Queue<object>(steps);

        public Task<StartInterviewResult> StartAsync(StartInterviewRequest r, CancellationToken ct = default)
            => Task.FromResult(new StartInterviewResult(Guid.NewGuid(), InterviewState.Discovery, "Q1"));

        public Task<SubmitAnswerResult> SubmitAnswerAsync(SubmitAnswerRequest r, CancellationToken ct = default)
            => Task.FromResult((SubmitAnswerResult)_steps.Dequeue());

        public Task<AdvanceResult> AdvanceAsync(Guid sessionId, CancellationToken ct = default)
            => Task.FromResult((AdvanceResult)_steps.Dequeue());
    }

    private static VoiceInterviewService Voice(IInterviewOrchestrator orch)
        => new(orch, NullLogger<VoiceInterviewService>.Instance);

    [Fact]
    public async Task NextTurn_within_a_round_returns_the_next_question()
    {
        var orch = new ScriptedOrchestrator(
            new SubmitAnswerResult(Guid.Empty, InterviewState.DeepDive, "Q2", RoundComplete: false));
        var turn = await Voice(orch).NextTurnAsync(Guid.NewGuid(), "an answer");

        Assert.Equal("Q2", turn.Utterance);
        Assert.False(turn.IsComplete);
    }

    [Fact]
    public async Task NextTurn_at_a_round_boundary_advances_and_returns_the_new_rounds_question()
    {
        var orch = new ScriptedOrchestrator(
            new SubmitAnswerResult(Guid.Empty, InterviewState.DeepDive, null, RoundComplete: true),
            new AdvanceResult(Guid.Empty, InterviewState.SystemDesign, IsTerminal: false, NextQuestion: "SD-Q1"));
        var turn = await Voice(orch).NextTurnAsync(Guid.NewGuid(), "an answer");

        Assert.Equal("SD-Q1", turn.Utterance);
        Assert.False(turn.IsComplete);
    }

    [Fact]
    public async Task NextTurn_at_the_end_completes_with_a_closing_remark()
    {
        var orch = new ScriptedOrchestrator(
            new SubmitAnswerResult(Guid.Empty, InterviewState.SystemDesign, null, RoundComplete: true),
            new AdvanceResult(Guid.Empty, InterviewState.Done, IsTerminal: true, NextQuestion: null));
        var turn = await Voice(orch).NextTurnAsync(Guid.NewGuid(), "an answer");

        Assert.True(turn.IsComplete);
        Assert.False(string.IsNullOrWhiteSpace(turn.Utterance)); // closing remark
    }
}
