// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace DbscSampleV2;

/// <summary>
/// Middleware that captures all HTTP requests/responses and writes them to a HAR file on disk.
/// This captures the full DBSC protocol flow including headers that Chrome DevTools may hide.
/// </summary>
public sealed class HarLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _outputPath;
    private readonly HarLog _harLog;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public HarLoggingMiddleware(RequestDelegate next, string outputPath)
    {
        _next = next;
        _outputPath = outputPath;
        _harLog = new HarLog
        {
            Log = new HarLogContent
            {
                Version = "1.2",
                Creator = new HarCreator { Name = "DbscSample", Version = "1.0" },
                Entries = [],
            }
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Capture request
        var requestHeaders = CaptureHeaders(context.Request.Headers);
        var requestBody = await CaptureRequestBodyAsync(context);

        // Buffer the response body
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            context.Response.Body = originalBodyStream;

            // Read captured response body
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBodyBytes = responseBodyStream.ToArray();
            var responseBody = Encoding.UTF8.GetString(responseBodyBytes);

            // Write response body to client
            if (responseBodyBytes.Length > 0)
            {
                await originalBodyStream.WriteAsync(responseBodyBytes);
            }

            // Capture response headers (after response has been written)
            var responseHeaders = CaptureHeaders(context.Response.Headers);

            var entry = new HarEntry
            {
                StartedDateTime = startTime.ToString("O"),
                Time = stopwatch.Elapsed.TotalMilliseconds,
                Request = new HarRequest
                {
                    Method = context.Request.Method,
                    Url = context.Request.GetDisplayUrl(),
                    HttpVersion = context.Request.Protocol,
                    Headers = requestHeaders,
                    HeadersSize = -1,
                    BodySize = requestBody?.Length ?? 0,
                    PostData = requestBody is not null ? new HarPostData
                    {
                        MimeType = context.Request.ContentType ?? "",
                        Text = requestBody,
                    } : null,
                },
                Response = new HarResponse
                {
                    Status = context.Response.StatusCode,
                    StatusText = "",
                    HttpVersion = context.Request.Protocol,
                    Headers = responseHeaders,
                    HeadersSize = -1,
                    BodySize = responseBodyBytes.Length,
                    Content = new HarContent
                    {
                        Size = responseBodyBytes.Length,
                        MimeType = context.Response.ContentType ?? "",
                        Text = responseBody,
                    },
                },
                Timings = new HarTimings
                {
                    Send = 0,
                    Wait = stopwatch.Elapsed.TotalMilliseconds,
                    Receive = 0,
                },
                Dbsc = BuildDbscAnnotations(requestHeaders, responseHeaders, requestBody),
            };

            lock (_lock)
            {
                _harLog.Log.Entries.Add(entry);
                var json = JsonSerializer.Serialize(_harLog, _jsonOptions);
                File.WriteAllText(_outputPath, json);
            }
        }
    }

    private static List<HarHeader> CaptureHeaders(IHeaderDictionary headers)
    {
        var result = new List<HarHeader>();
        foreach (var header in headers)
        {
            foreach (var value in header.Value)
            {
                result.Add(new HarHeader { Name = header.Key, Value = value ?? "" });
            }
        }
        return result;
    }

    private static async Task<string?> CaptureRequestBodyAsync(HttpContext context)
    {
        if (context.Request.ContentLength is null or 0)
        {
            return null;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        return body;
    }

    // === DBSC decoding helpers ===
    // Adds a non-standard "_dbsc" object to each entry (HAR viewers ignore underscore-prefixed fields).
    // Decodes the DBSC proof JWT (base64url JSON, no key needed), parses DBSC headers, and breaks down
    // cookies. Auth cookie VALUES are ASP.NET Core Data Protection ciphertext, so only their names,
    // sizes, and Set-Cookie attributes are surfaced (the plaintext requires the server keyring).
    private static DbscAnnotations? BuildDbscAnnotations(
        List<HarHeader> requestHeaders,
        List<HarHeader> responseHeaders,
        string? requestBody)
    {
        string? Find(List<HarHeader> headers, string name) =>
            headers.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;

        var annotations = new DbscAnnotations();

        // Request cookies (names + lengths; values are encrypted)
        var cookieHeader = Find(requestHeaders, "cookie");
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            var cookies = new List<DbscCookie>();
            foreach (var pair in cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var eq = pair.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }
                cookies.Add(new DbscCookie
                {
                    Name = pair[..eq],
                    ValueLength = pair.Length - eq - 1,
                });
            }
            annotations.RequestCookies = cookies.Count > 0 ? cookies : null;
        }

        // Set-Cookie responses (name + attributes; values are encrypted)
        var setCookies = responseHeaders
            .Where(h => string.Equals(h.Name, "set-cookie", StringComparison.OrdinalIgnoreCase))
            .Select(h => ParseSetCookie(h.Value))
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();
        annotations.SetCookies = setCookies.Count > 0 ? setCookies : null;

        // DBSC proof JWT: carried in the Secure-Session-Response request header (DBSC v2),
        // with the empty POST body fallback for robustness.
        var proofRaw = Find(requestHeaders, "secure-session-response");
        if (string.IsNullOrEmpty(proofRaw) && requestBody is { Length: > 0 } && requestBody.Count(c => c == '.') == 2)
        {
            proofRaw = requestBody;
        }
        annotations.ProofJwt = TryDecodeJwt(proofRaw);

        // DBSC negotiation headers (raw structured-field values, plus the challenge id when present)
        annotations.RegistrationHeader = Find(responseHeaders, "secure-session-registration");
        var challenge = Find(responseHeaders, "secure-session-challenge");
        if (!string.IsNullOrEmpty(challenge))
        {
            annotations.ChallengeHeader = challenge;
            var idIdx = challenge.IndexOf("id=", StringComparison.OrdinalIgnoreCase);
            if (idIdx >= 0)
            {
                annotations.ChallengeSessionId = challenge[(idIdx + 3)..].Trim().Trim('"');
            }
        }
        annotations.SessionId = Find(requestHeaders, "sec-secure-session-id")?.Trim().Trim('"');

        var hasContent = annotations.RequestCookies is not null
            || annotations.SetCookies is not null
            || annotations.ProofJwt is not null
            || annotations.RegistrationHeader is not null
            || annotations.ChallengeHeader is not null
            || annotations.SessionId is not null;

        return hasContent ? annotations : null;
    }

    private static DbscSetCookie? ParseSetCookie(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var segments = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        var first = segments[0];
        var eq = first.IndexOf('=');
        if (eq <= 0)
        {
            return null;
        }

        var attributes = segments.Length > 1
            ? string.Join("; ", segments[1..])
            : null;

        return new DbscSetCookie
        {
            Name = first[..eq],
            ValueLength = first.Length - eq - 1,
            Attributes = attributes,
        };
    }

    private static DbscJwt? TryDecodeJwt(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        token = token.Trim().Trim('"');
        var parts = token.Split('.');
        if (parts.Length is < 2 or > 3)
        {
            return null;
        }

        var headerBytes = TryBase64UrlDecode(parts[0]);
        var payloadBytes = TryBase64UrlDecode(parts[1]);
        if (headerBytes is null || payloadBytes is null)
        {
            return null;
        }

        try
        {
            return new DbscJwt
            {
                Header = JsonNode.Parse(headerBytes),
                Payload = JsonNode.Parse(payloadBytes),
                SignaturePresent = parts.Length == 3 && parts[2].Length > 0,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static byte[]? TryBase64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            case 1: return null;
        }
        try
        {
            return Convert.FromBase64String(s);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

// HAR format models
public sealed class HarLog
{
    [JsonPropertyName("log")]
    public HarLogContent Log { get; set; } = null!;
}

public sealed class HarLogContent
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.2";

    [JsonPropertyName("creator")]
    public HarCreator Creator { get; set; } = null!;

    [JsonPropertyName("entries")]
    public List<HarEntry> Entries { get; set; } = [];
}

public sealed class HarCreator
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}

public sealed class HarEntry
{
    [JsonPropertyName("startedDateTime")]
    public string StartedDateTime { get; set; } = "";

    [JsonPropertyName("time")]
    public double Time { get; set; }

    [JsonPropertyName("request")]
    public HarRequest Request { get; set; } = null!;

    [JsonPropertyName("response")]
    public HarResponse Response { get; set; } = null!;

    [JsonPropertyName("timings")]
    public HarTimings Timings { get; set; } = null!;

    [JsonPropertyName("_dbsc")]
    public DbscAnnotations? Dbsc { get; set; }
}

// DBSC decoding annotations (non-standard "_dbsc" HAR extension).
public sealed class DbscAnnotations
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("proofJwt")]
    public DbscJwt? ProofJwt { get; set; }

    [JsonPropertyName("registrationHeader")]
    public string? RegistrationHeader { get; set; }

    [JsonPropertyName("challengeHeader")]
    public string? ChallengeHeader { get; set; }

    [JsonPropertyName("challengeSessionId")]
    public string? ChallengeSessionId { get; set; }

    [JsonPropertyName("requestCookies")]
    public List<DbscCookie>? RequestCookies { get; set; }

    [JsonPropertyName("setCookies")]
    public List<DbscSetCookie>? SetCookies { get; set; }
}

public sealed class DbscJwt
{
    [JsonPropertyName("header")]
    public JsonNode? Header { get; set; }

    [JsonPropertyName("payload")]
    public JsonNode? Payload { get; set; }

    [JsonPropertyName("signaturePresent")]
    public bool SignaturePresent { get; set; }
}

public sealed class DbscCookie
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("valueLength")]
    public int ValueLength { get; set; }
}

public sealed class DbscSetCookie
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("valueLength")]
    public int ValueLength { get; set; }

    [JsonPropertyName("attributes")]
    public string? Attributes { get; set; }
}

public sealed class HarRequest
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("httpVersion")]
    public string HttpVersion { get; set; } = "";

    [JsonPropertyName("headers")]
    public List<HarHeader> Headers { get; set; } = [];

    [JsonPropertyName("headersSize")]
    public int HeadersSize { get; set; }

    [JsonPropertyName("bodySize")]
    public int BodySize { get; set; }

    [JsonPropertyName("postData")]
    public HarPostData? PostData { get; set; }
}

public sealed class HarResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusText")]
    public string StatusText { get; set; } = "";

    [JsonPropertyName("httpVersion")]
    public string HttpVersion { get; set; } = "";

    [JsonPropertyName("headers")]
    public List<HarHeader> Headers { get; set; } = [];

    [JsonPropertyName("headersSize")]
    public int HeadersSize { get; set; }

    [JsonPropertyName("bodySize")]
    public int BodySize { get; set; }

    [JsonPropertyName("content")]
    public HarContent Content { get; set; } = null!;
}

public sealed class HarHeader
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

public sealed class HarPostData
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public sealed class HarContent
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public sealed class HarTimings
{
    [JsonPropertyName("send")]
    public double Send { get; set; }

    [JsonPropertyName("wait")]
    public double Wait { get; set; }

    [JsonPropertyName("receive")]
    public double Receive { get; set; }
}
