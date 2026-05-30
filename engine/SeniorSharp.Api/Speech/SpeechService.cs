using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SeniorSharp.Api;

/// <summary>
/// Speech I/O for the voice mode, backed by OpenAI: speech-to-text (gpt-4o-transcribe) and
/// text-to-speech (gpt-4o-mini-tts). The candidate's transcribed words drive the same interview engine;
/// the verdict is always computed from the transcript, so voice is purely an I/O layer.
/// </summary>
public sealed class SpeechService
{
    private const string SttModel = "gpt-4o-transcribe";
    private const string TtsModel = "gpt-4o-mini-tts";
    private const string TtsVoice = "alloy";

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public SpeechService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Voice:ApiKey"] ?? string.Empty;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>Transcribes an uploaded audio stream to text via OpenAI.</summary>
    public async Task<string> TranscribeAsync(Stream audio, string? contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(audio);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "audio/webm" : contentType.Split(';')[0]);
        form.Add(fileContent, "file", FileNameFor(contentType));
        form.Add(new StringContent(SttModel), "model");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = form;

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI STT failed ({(int)resp.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
    }

    /// <summary>Synthesizes speech (MP3 bytes) from text via OpenAI.</summary>
    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            model = TtsModel,
            input = text,
            voice = TtsVoice,
            response_format = "mp3",
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/speech");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"OpenAI TTS failed ({(int)resp.StatusCode}): {await resp.Content.ReadAsStringAsync(ct)}");

        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    private static string FileNameFor(string? contentType)
    {
        var ct = (contentType ?? string.Empty).ToLowerInvariant();
        if (ct.Contains("webm")) return "audio.webm";
        if (ct.Contains("ogg")) return "audio.ogg";
        if (ct.Contains("wav")) return "audio.wav";
        if (ct.Contains("mp4") || ct.Contains("m4a") || ct.Contains("aac")) return "audio.mp4";
        if (ct.Contains("mpeg") || ct.Contains("mp3")) return "audio.mp3";
        return "audio.webm";
    }
}
