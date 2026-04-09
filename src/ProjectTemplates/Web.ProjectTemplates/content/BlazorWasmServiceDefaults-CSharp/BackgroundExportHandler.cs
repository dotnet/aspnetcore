// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Polly;

namespace BlazorWasm.ServiceDefaults1;

/// <summary>
/// A DelegatingHandler that works around the OTel SDK's sync-over-async
/// deadlock on WASM. The SDK calls SendAsync().GetAwaiter().GetResult()
/// in OtlpExportClient.SendHttpRequest(), which blocks the single WASM
/// thread. This handler returns 200 immediately to unblock the SDK,
/// then sends the real request with retries in the background.
/// </summary>
internal sealed class BackgroundExportHandler(
    ResiliencePipeline<HttpResponseMessage> pipeline,
    ILogger logger) : DelegatingHandler(new HttpClientHandler())
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Capture request data before returning — the SDK disposes the
        // HttpRequestMessage via 'using' after .GetResult() completes.
        var snapshot = RequestSnapshot.Capture(request);

        // Send the real request with retries in the background.
        _ = SendWithRetryAsync(snapshot, cancellationToken);

        // Return 200 immediately so the SDK's sync .GetResult() unblocks.
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private async Task SendWithRetryAsync(
        RequestSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            var response = await pipeline.ExecuteAsync(async token =>
            {
                using var clone = snapshot.CreateRequest();
                return await base.SendAsync(clone, token).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "OTLP export to {Uri} completed with status {StatusCode} after retries.",
                    snapshot.RequestUri, response.StatusCode);
            }

            response.Dispose();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "OTLP export to {Uri} failed after retries.", snapshot.RequestUri);
        }
    }
}

/// <summary>
/// Captures the essential parts of an HttpRequestMessage so it can be
/// cloned for each retry attempt after the original request is disposed
/// by the SDK. The OTLP SDK always sends ByteArrayContent (protobuf),
/// so ReadAsByteArrayAsync completes synchronously.
/// </summary>
internal sealed class RequestSnapshot
{
    public HttpMethod Method { get; init; } = null!;
    public Uri RequestUri { get; init; } = null!;
    public List<KeyValuePair<string, IEnumerable<string>>> Headers { get; init; } = null!;
    public byte[]? ContentBytes { get; init; }
    public MediaTypeHeaderValue? ContentType { get; init; }

    public static RequestSnapshot Capture(HttpRequestMessage request)
    {
        byte[]? contentBytes = null;
        MediaTypeHeaderValue? contentType = null;

        if (request.Content is not null)
        {
            // ByteArrayContent.ReadAsByteArrayAsync completes synchronously
            // since the bytes are already in memory — safe to .GetResult().
            contentBytes = request.Content.ReadAsByteArrayAsync()
                .GetAwaiter().GetResult();
            contentType = request.Content.Headers.ContentType;
        }

        // Copy headers since the original request will be disposed.
        var headers = new List<KeyValuePair<string, IEnumerable<string>>>();
        foreach (var header in request.Headers)
        {
            headers.Add(new(header.Key, header.Value.ToArray()));
        }

        return new RequestSnapshot
        {
            Method = request.Method,
            RequestUri = request.RequestUri!,
            Headers = headers,
            ContentBytes = contentBytes,
            ContentType = contentType,
        };
    }

    public HttpRequestMessage CreateRequest()
    {
        var clone = new HttpRequestMessage(Method, RequestUri);

        foreach (var header in Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (ContentBytes is not null)
        {
            clone.Content = new ByteArrayContent(ContentBytes);
            if (ContentType is not null)
            {
                clone.Content.Headers.ContentType = ContentType;
            }
        }

        return clone;
    }
}
