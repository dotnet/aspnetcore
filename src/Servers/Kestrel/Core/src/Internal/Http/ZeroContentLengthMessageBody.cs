// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal sealed class ZeroContentLengthMessageBody : MessageBody
{
    public ZeroContentLengthMessageBody(bool keepAlive)
        : base(null!) // Ok to pass null here because this type overrides all the base methods
    {
        RequestKeepAlive = keepAlive;
    }

    public override bool IsEmpty => true;

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => new ValueTask<ReadResult>(new ReadResult(default, isCanceled: false, isCompleted: true));

    public override Task ConsumeAsync() => Task.CompletedTask;

    public override ValueTask StopAsync() => default;

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { }

    public override bool TryRead(out ReadResult result)
    {
        result = new ReadResult(default, isCanceled: false, isCompleted: true);
        return true;
    }

    public override void Complete(Exception? ex) { }

    public override void CancelPendingRead() { }
}
