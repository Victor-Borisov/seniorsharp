using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Options;

namespace SeniorSharp.Orchestration;

/// <summary>
/// <see cref="IPromptProvider"/> that reads <c>{PromptsDir}/{role}.{version}.md</c> from disk and
/// caches the contents in memory (prompt files are immutable for a given version).
/// </summary>
public sealed class FilePromptProvider : IPromptProvider
{
    private readonly PromptOptions _options;
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public FilePromptProvider(IOptions<PromptOptions> options) => _options = options.Value;

    public string GetSystemPrompt(string role, string version)
    {
        return _cache.GetOrAdd($"{role}.{version}", key =>
        {
            var path = Path.GetFullPath(Path.Combine(_options.PromptsDir, $"{key}.md"));
            if (!File.Exists(path))
                throw new FileNotFoundException($"Prompt '{key}' not found at '{path}'.", path);

            return File.ReadAllText(path);
        });
    }

    public string GetCriteria()
    {
        return _cache.GetOrAdd("__criteria__", _ =>
        {
            var path = Path.GetFullPath(_options.CriteriaPath);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Criteria not found at '{path}'.", path);

            return File.ReadAllText(path);
        });
    }
}
