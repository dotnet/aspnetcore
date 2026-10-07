// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestHttpResponseControl : IHttpResponseControl
{
    private byte[] _memory = new byte[4096];

    public Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<FlushResult>> WritePipeAsyncCallback { get; set; }
        = (_, _) => new ValueTask<FlushResult>(new FlushResult());

    public long UnflushedBytes { get; set; }

    public ValueTask<FlushResult> ProduceContinueAsync() => new ValueTask<FlushResult>(new FlushResult());

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_memory.Length < sizeHint)
        {
            _memory = new byte[sizeHint];
        }

        return _memory;
    }

    public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

    public void Advance(int bytes)
    {
    }

    public ValueTask<FlushResult> FlushPipeAsync(CancellationToken cancellationToken) => new ValueTask<FlushResult>(new FlushResult());

    public ValueTask<FlushResult> WritePipeAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        => WritePipeAsyncCallback(source, cancellationToken);

    public void CancelPendingFlush()
    {
    }

    public Task CompleteAsync(Exception exception = null) => Task.CompletedTask;
}
