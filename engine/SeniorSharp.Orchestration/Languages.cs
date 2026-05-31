using System.Collections.Generic;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Supported interview languages (locale code -> human-readable name). The chosen language is recorded on
/// the session and injected into every role prompt so the interview and verdict are produced in it.
/// </summary>
public static class Languages
{
    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        ["am"] = "Amharic", ["ar"] = "Arabic", ["bg"] = "Bulgarian", ["bn"] = "Bengali",
        ["ca"] = "Catalan", ["cs"] = "Czech", ["da"] = "Danish", ["de"] = "German",
        ["el"] = "Greek", ["en"] = "English", ["es"] = "Spanish", ["es_419"] = "Spanish (Latin America)",
        ["et"] = "Estonian", ["fa"] = "Persian", ["fi"] = "Finnish", ["fil"] = "Filipino",
        ["fr"] = "French", ["gu"] = "Gujarati", ["he"] = "Hebrew", ["hi"] = "Hindi",
        ["hr"] = "Croatian", ["hu"] = "Hungarian", ["id"] = "Indonesian", ["it"] = "Italian",
        ["ja"] = "Japanese", ["kn"] = "Kannada", ["ko"] = "Korean", ["lt"] = "Lithuanian",
        ["lv"] = "Latvian", ["ml"] = "Malayalam", ["mr"] = "Marathi", ["ms"] = "Malay",
        ["nl"] = "Dutch", ["no"] = "Norwegian", ["pl"] = "Polish", ["pt_BR"] = "Portuguese (Brazil)",
        ["pt_PT"] = "Portuguese (Portugal)", ["ro"] = "Romanian", ["ru"] = "Russian", ["sk"] = "Slovak",
        ["sl"] = "Slovenian", ["sr"] = "Serbian", ["sv"] = "Swedish", ["sw"] = "Swahili",
        ["ta"] = "Tamil", ["te"] = "Telugu", ["th"] = "Thai", ["tr"] = "Turkish",
        ["uk"] = "Ukrainian", ["vi"] = "Vietnamese", ["zh_CN"] = "Chinese (Simplified)",
        ["zh_TW"] = "Chinese (Traditional)",
    };

    /// <summary>Human-readable language name for a locale code; defaults to English for unknown/empty codes.</summary>
    public static string NameOf(string? code)
        => code is not null && Names.TryGetValue(code, out var name) ? name : "English";
}
