// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <remarks>
/// Used to plug HTTP version-specific functionality into <see cref="HttpProtocol"/>.
/// </remarks>
internal interface IHttpOutputProducer
{
    ValueTask<FlushResult> WriteChunkAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken);
    ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken);
    ValueTask<FlushResult> Write100ContinueAsync();
    void WriteResponseHeaders(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, bool appCompleted);
    // This takes ReadOnlySpan instead of ReadOnlyMemory because it always synchronously copies data before flushing.
    ValueTask<FlushResult> WriteDataToPipeAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken);
    // Test hook
    Task WriteDataAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken);
    ValueTask<FlushResult> WriteStreamSuffixAsync();
    void Advance(int bytes);
    long UnflushedBytes { get; }
    Span<byte> GetSpan(int sizeHint = 0);
    Memory<byte> GetMemory(int sizeHint = 0);
    void CancelPendingFlush();
    void Stop();
    ValueTask<FlushResult> FirstWriteAsync(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken);
    ValueTask<FlushResult> FirstWriteChunkedAsync(int statusCode, string? reasonPhrase, HttpResponseHeaders responseHeaders, bool autoChunk, ReadOnlySpan<byte> data, CancellationToken cancellationToken);
    void Reset();
}
