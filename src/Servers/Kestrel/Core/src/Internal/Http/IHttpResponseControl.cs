// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal interface IHttpResponseControl
{
    ValueTask<FlushResult> ProduceContinueAsync();
    Memory<byte> GetMemory(int sizeHint = 0);
    Span<byte> GetSpan(int sizeHint = 0);
    void Advance(int bytes);
    long UnflushedBytes { get; }
    ValueTask<FlushResult> FlushPipeAsync(CancellationToken cancellationToken);
    ValueTask<FlushResult> WritePipeAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken);
    void CancelPendingFlush();
    Task CompleteAsync(Exception? exception = null);
}
