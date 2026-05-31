using System;
using Anthropic;
using Anthropic.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SeniorSharp.Llm;

/// <summary>
/// DI registration for the SeniorSharp LLM provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AnthropicOptions"/>, the underlying <see cref="AnthropicClient"/>,
    /// and <see cref="ILlmClient"/> (Anthropic-backed).
    /// </summary>
    public static IServiceCollection AddSeniorSharpLlm(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;

            // Raise the HTTP timeout well above the SDK default (100s): large structured calls — especially
            // the scorer over a full transcript — can take longer and would otherwise be cancelled -> 500.
            var clientOptions = new ClientOptions { Timeout = TimeSpan.FromMinutes(5) };

            // An explicit key from configuration (.env / user-secrets / appsettings) wins; when empty the
            // SDK falls back to the ANTHROPIC_API_KEY environment variable on its own.
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                clientOptions.APIKey = options.ApiKey;

            return new AnthropicClient(clientOptions);
        });

        services.AddSingleton<ILlmClient, AnthropicLlmClient>();

        return services;
    }
}
