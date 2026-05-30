using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SeniorSharp.Orchestration;

/// <summary>DI registration for the orchestration layer.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the interview roles (questioner/classifier/scorer), the FSM-driven
    /// orchestrator, the prompt provider and supporting services. Requires <c>AddSeniorSharpLlm</c>
    /// and <c>AddSeniorSharpPersistence</c> to have been called.
    /// </summary>
    public static IServiceCollection AddSeniorSharpOrchestration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PromptOptions>(configuration.GetSection(PromptOptions.SectionName));
        services.Configure<InterviewOptions>(configuration.GetSection(InterviewOptions.SectionName));
        services.AddSingleton<IPromptProvider, FilePromptProvider>();

        services.AddScoped<IQuestioner, Questioner>();
        services.AddScoped<IClassifier, Classifier>();
        services.AddScoped<IScorer, Scorer>();
        services.AddScoped<IInterviewOrchestrator, InterviewOrchestrator>();

        // Turn-oriented facade for managed voice providers (STT/TTS handled by the provider).
        services.AddScoped<IVoiceInterview, VoiceInterviewService>();

        // Simulation/eval scaffolding (not a production interview role): lets the interview loop
        // run end-to-end without a human and backs the M4 autoeval.
        services.AddScoped<ICandidate, SimulatedCandidate>();
        services.AddScoped<InterviewSimulator>();
        return services;
    }
}
