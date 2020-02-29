// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared
{
    public class TestDuplexPipe : IDuplexPipe
    {
        private readonly TestPipeReader _input;

        public PipeReader Input => _input;

        public PipeWriter Output { get; }

        public TestDuplexPipe(bool writerForceAsync = false)
        {
            _input = new TestPipeReader();
            Output = new TestPipeWriter
            {
                ForceAsync = writerForceAsync
            };
        }

        public void AddReadResult(ValueTask<ReadResult> readResult)
        {
            _input.ReadResults.Add(readResult);
        }
    }
}