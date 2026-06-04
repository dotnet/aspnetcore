// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace DbscSample;

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
