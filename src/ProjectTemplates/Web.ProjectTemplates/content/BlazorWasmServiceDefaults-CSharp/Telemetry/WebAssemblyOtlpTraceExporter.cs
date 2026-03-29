// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.Headers;
using BlazorWasm.ServiceDefaults1.Telemetry.Serializer;
using OpenTelemetry;

namespace BlazorWasm.ServiceDefaults1.Telemetry;

public sealed class WebAssemblyOtlpTraceExporter : BaseExporter<Activity>
{
    private static readonly MediaTypeHeaderValue s_protobufMediaType = new("application/x-protobuf");

    private readonly Uri _endpoint;
    private readonly HttpClient _httpClient;
    private readonly string _serviceName;
    private readonly Dictionary<string, string>? _headers;

    public WebAssemblyOtlpTraceExporter(Uri endpoint, string serviceName, Dictionary<string, string>? headers = null, HttpClient? httpClient = null)
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
    public override ExportResult Export(in Batch<Activity> batch)
    {
        // Convert batch to a list we can enumerate multiple times
        var activities = new List<Activity>();
        foreach (var activity in batch)
        {
            activities.Add(activity);
        }

        if (activities.Count == 0)
        {
            return ExportResult.Success;
        }

        try
        {
            // Serialize to OTLP protobuf format
            var protobufPayload = OtlpTraceSerializer.SerializeTraceData(activities, _serviceName);

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
