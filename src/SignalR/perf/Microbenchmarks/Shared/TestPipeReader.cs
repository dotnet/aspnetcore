// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;

public class TestPipeReader : PipeReader
{
    public List<ValueTask<ReadResult>> ReadResults { get; }

    public TestPipeReader()
    {
        ReadResults = new List<ValueTask<ReadResult>>();
    }

    public override void AdvanceTo(SequencePosition consumed)
    {
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
    }

    public override void CancelPendingRead()
    {
        throw new NotImplementedException();
    }

    public override void Complete(Exception exception = null)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        if (ReadResults.Count == 0)
        {
            return new ValueTask<ReadResult>(new ReadResult(default, false, true));
        }

        var result = ReadResults[0];
        ReadResults.RemoveAt(0);

        return result;
    }

    public override bool TryRead(out ReadResult result)
    {
        throw new NotImplementedException();
    }
}
