// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Xunit.Abstractions;

namespace InteropTests.Helpers
{
    public class WebServerProcess : IDisposable
    {
        private readonly Process _process;
        private readonly ProcessEx _processEx;
        private readonly TaskCompletionSource<object> _startTcs;

        public WebServerProcess(string path, ITestOutputHelper output)
        {
            _process = new Process();
            _process.StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "dotnet.exe",
                Arguments = @$"run -p {path}"
            };
            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += Process_OutputDataReceived;
            _process.Start();

            _processEx = new ProcessEx(output, _process);

            _startTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task WaitForReady()
        {
            if (_processEx.HasExited)
            {
                return Task.FromException(new InvalidOperationException("Server is not running."));
            }

            return _startTcs.Task;
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var data = e.Data;
            if (data != null)
            {
                if (data.Contains("Application started."))
                {
                    _startTcs.TrySetResult(null);
                }
            }
        }

        public void Dispose()
        {
            _processEx.Dispose();
        }
    }
}
