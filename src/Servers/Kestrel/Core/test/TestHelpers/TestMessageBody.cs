// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal class TestMessageBody : MessageBody
{
    public TestMessageBody()
        : base(null)
    {
    }

    public Func<CancellationToken, ValueTask<ReadResult>> ReadAsyncFunc { get; set; }
        = _ => new ValueTask<ReadResult>(new ReadResult(default, isCanceled: false, isCompleted: true));

    public int ConsumeAsyncCount { get; private set; }

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => ReadAsyncFunc(cancellationToken);

    public override bool TryRead(out ReadResult readResult)
    {
        readResult = default;
        return false;
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
    }

    public override void CancelPendingRead()
    {
    }

    public override void Complete(Exception exception)
    {
    }

    public override Task ConsumeAsync()
    {
        ConsumeAsyncCount++;
        return Task.CompletedTask;
    }
}
