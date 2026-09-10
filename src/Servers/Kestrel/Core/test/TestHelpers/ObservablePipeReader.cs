// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests.TestHelpers;

internal class ObservablePipeReader : PipeReader
{
    private readonly PipeReader _inner;

    public ObservablePipeReader(PipeReader reader)
    {
        _inner = reader;
    }

    /// <summary>
    /// Number of times <see cref="ReadAsync(CancellationToken)"/> was called.
    /// </summary>
    public int ReadAsyncCounter { get; private set; }

    public override void AdvanceTo(SequencePosition consumed)
        => _inner.AdvanceTo(consumed);

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        => _inner.AdvanceTo(consumed, examined);

    public override void CancelPendingRead()
        => _inner.CancelPendingRead();

    public override void Complete(Exception exception = null)
        => _inner.Complete(exception);

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        ReadAsyncCounter++;
        return _inner.ReadAsync(cancellationToken);
    }

    public override bool TryRead(out ReadResult result)
    {
        return _inner.TryRead(out result);
    }
}
