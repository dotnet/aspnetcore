// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using BlazorWasm.ServiceDefaults1.Telemetry.Serializer;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace BlazorWasm.ServiceDefaults1.Telemetry;

public sealed class WebAssemblyOtlpLogExporter : BaseExporter<LogRecord>
{
    private static readonly MediaTypeHeaderValue s_protobufMediaType = new("application/x-protobuf");

    private readonly Uri _endpoint;
    private readonly HttpClient _httpClient;
    private readonly string _serviceName;
    private readonly Dictionary<string, string>? _headers;

    public WebAssemblyOtlpLogExporter(Uri endpoint, string serviceName, Dictionary<string, string>? headers = null, HttpClient? httpClient = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _headers = headers;
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        // Convert batch to our simplified data format - we need to capture the data
        // before returning because the LogRecord objects will be pooled/recycled
        var logRecords = new List<LogRecordData>();
        foreach (var logRecord in batch)
        {
            var data = CaptureLogRecord(logRecord);
            logRecords.Add(data);
        }

        if (logRecords.Count == 0)
        {
            return ExportResult.Success;
        }

        try
        {
            // Serialize to OTLP protobuf format
            var protobufPayload = OtlpLogSerializer.SerializeLogsData(logRecords, _serviceName);

            // Fire-and-forget the HTTP call - this is the key difference from the standard exporter
            // We don't block on the result, which avoids the WebAssembly single-thread deadlock
            SendAsync(protobufPayload);

            return ExportResult.Success;
        }
        catch (Exception)
        {
            return ExportResult.Failure;
        }
    }

    private static LogRecordData CaptureLogRecord(LogRecord logRecord)
    {
        // Capture attributes
        List<KeyValuePair<string, object?>>? attributes = null;
        if (logRecord.Attributes != null)
        {
            attributes = new List<KeyValuePair<string, object?>>();
            foreach (var attr in logRecord.Attributes)
            {
                attributes.Add(new KeyValuePair<string, object?>(attr.Key, attr.Value?.ToString()));
            }
        }

        return new LogRecordData
        {
            Timestamp = logRecord.Timestamp,
            CategoryName = logRecord.CategoryName,
            LogLevel = logRecord.LogLevel,
            EventId = logRecord.EventId,
            FormattedMessage = logRecord.FormattedMessage ?? logRecord.Body,
            Exception = logRecord.Exception,
            Attributes = attributes,
            TraceId = logRecord.TraceId,
            SpanId = logRecord.SpanId,
            TraceFlags = logRecord.TraceFlags
        };
    }

    private async void SendAsync(byte[] payload)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Content = new ByteArrayContent(payload);
            request.Content.Headers.ContentType = s_protobufMediaType;

            // Add custom headers (e.g., x-otlp-api-key for authentication)
            if (_headers is not null)
            {
                foreach (var header in _headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            await _httpClient.SendAsync(request).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // SendAsync failed - fire and forget pattern
        }
    }

    /// <inheritdoc/>
    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        return true;
    }
}
