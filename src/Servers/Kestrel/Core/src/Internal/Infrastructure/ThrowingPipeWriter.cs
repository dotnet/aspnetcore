// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class ThrowingPipeWriter : PipeWriter
{
    private readonly string _message;

    public ThrowingPipeWriter(string message)
    {
        _message = message;
    }

    public override void Advance(int bytes) => throw new InvalidOperationException(_message);

    public override void CancelPendingFlush() => throw new InvalidOperationException(_message);

    public override void Complete(Exception? exception = null) => throw new InvalidOperationException(_message);

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException(_message);

    public override Memory<byte> GetMemory(int sizeHint = 0) => throw new InvalidOperationException(_message);

    public override Span<byte> GetSpan(int sizeHint = 0) => throw new InvalidOperationException(_message);
}
