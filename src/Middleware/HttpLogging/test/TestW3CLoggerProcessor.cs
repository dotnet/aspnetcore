// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal sealed class TestW3CLoggerProcessor : W3CLoggerProcessor
    {
        private int _writeCount = 0;
        private int _expectedWrites;
        private TaskCompletionSource _tcs;
        private bool _hasWritten;
        private readonly object _writeCountLock = new object();
        private string _internalMessage;

        public TestW3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory)
        {
            Lines = new List<string>();
        }

        public List<string> Lines { get; }

        internal override StreamWriter GetStreamWriter(string fileName)
        {
            return StreamWriter.Null;
        }

        internal override void OnWriteLine(string message)
        {
            // Add the previous message formatted
            if (!string.IsNullOrWhiteSpace(_internalMessage))
            {
                Lines.Add(_internalMessage);

                // reset the internal message
                _internalMessage = string.Empty;
            }

            Lines.Add(message);
            lock (_writeCountLock)
            {
                _writeCount++;
                if (_tcs != null && _writeCount >= _expectedWrites)
                {
                    _tcs.SetResult();
                }
            }
        }

        internal override void OnWrite(string message)
        {
            if (string.IsNullOrWhiteSpace(_internalMessage))
            {
                _internalMessage = message;
            }
            else
            {
                _internalMessage = string.Concat(_internalMessage, message);
            }
        }

        public Task WaitForWrites(int numWrites)
        {
            lock (_writeCountLock)
            {
                if (_writeCount >= numWrites)
                {
                    return Task.CompletedTask;
                }
                _expectedWrites = numWrites;
                _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            return _tcs.Task;
        }

        public override async Task OnFirstWrite(StreamWriter streamWriter, CancellationToken cancellationToken)
        {
            if (!_hasWritten)
            {
                await base.OnFirstWrite(streamWriter, cancellationToken);
                _hasWritten = true;
            }
        }
    }
}
