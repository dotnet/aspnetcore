// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal sealed class TestW3CLoggerProcessor : W3CLoggerProcessor
    {
        public int WriteCount = 0;
        private int _expectedWrites;
        private TaskCompletionSource _tcs;
        public List<string> Lines;
        private bool _hasWritten;
        private readonly object _lockObj = new object();

        public TestW3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory)
        {
            Lines = new List<string>();
        }

        internal override StreamWriter GetStreamWriter(string fileName)
        {
            return StreamWriter.Null;
        }

        internal override void OnWrite(string message)
        {
            Lines.Add(message);
            WriteCount++;
            lock (_lockObj)
            {
                if (_tcs != null && WriteCount >= _expectedWrites)
                {
                    _tcs.SetResult();
                }
            }
        }

        public Task WaitForWrites(int numWrites)
        {
            lock (_lockObj)
            {
                _expectedWrites = numWrites;
                _tcs = new TaskCompletionSource();
            }
            return _tcs.Task;
        }

        public override async Task OnFirstWrite(StreamWriter streamWriter)
        {
            if (!_hasWritten)
            {
                await base.OnFirstWrite(streamWriter);
                _hasWritten = true;
            }
        }
    }
}
