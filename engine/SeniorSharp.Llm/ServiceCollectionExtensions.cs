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

            // An explicit key from configuration (.env / user-secrets / appsettings) wins. When it is
            // empty the official SDK falls back to the ANTHROPIC_API_KEY environment variable on its own,
            // so a plain `new AnthropicClient()` still works in that case.
            return string.IsNullOrWhiteSpace(options.ApiKey)
                ? new AnthropicClient()
                : new AnthropicClient(new ClientOptions { APIKey = options.ApiKey });
        });

        services.AddSingleton<ILlmClient, AnthropicLlmClient>();

        return services;
    }
}
