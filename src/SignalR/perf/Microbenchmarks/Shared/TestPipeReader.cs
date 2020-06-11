// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared
{
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
}
