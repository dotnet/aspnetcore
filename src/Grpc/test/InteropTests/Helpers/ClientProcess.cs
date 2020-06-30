// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Xunit.Abstractions;

namespace InteropTests.Helpers
{
    public class ClientProcess : IDisposable
    {
        private readonly Process _process;
        private readonly ProcessEx _processEx;
        private readonly TaskCompletionSource<object> _startTcs;
        private readonly StringBuilder _output;
        private readonly object _outputLock = new object();

        public ClientProcess(ITestOutputHelper output, string path, string serverPort, string testCase)
        {
            _output = new StringBuilder();

            _process = new Process();
            _process.StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "dotnet",
                Arguments = @$"{path} --use_tls false --server_port {serverPort} --client_type httpclient --test_case {testCase}"
            };
            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += Process_OutputDataReceived;
            _process.ErrorDataReceived += Process_ErrorDataReceived;
            _process.Start();

            _processEx = new ProcessEx(output, _process, timeout: Timeout.InfiniteTimeSpan);

            _startTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task WaitForReadyAsync() => _startTcs.Task;
        public Task WaitForExitAsync() => _processEx.Exited;
        public int ExitCode => _process.ExitCode;
        public bool IsReady => _startTcs.Task.IsCompletedSuccessfully;

        public string GetOutput()
        {
            lock (_outputLock)
            {
                return _output.ToString();
            }
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

                lock (_outputLock)
                {
                    _output.AppendLine(data);
                }
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var data = e.Data;
            if (data != null)
            {
                lock (_outputLock)
                {
                    _output.AppendLine("ERROR: " + data);
                }
            }
        }

        public void Dispose()
        {
            _processEx.Dispose();
        }
    }
}
