// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Xunit.Abstractions;

namespace InteropTests.Helpers
{
    public class WebsiteProcess : IDisposable
    {
        private readonly Process _process;
        private readonly ProcessEx _processEx;
        private readonly TaskCompletionSource<object> _startTcs;
        private static readonly Regex NowListeningRegex = new Regex(@"^\s*Now listening on: .*:(?<port>\d*)$");
        private readonly StringBuilder _output;
        private readonly object _outputLock = new object();

        public string ServerPort { get; private set; }
        public bool IsReady => _startTcs.Task.IsCompletedSuccessfully;

        public WebsiteProcess(string path, ITestOutputHelper output)
        {
            _output = new StringBuilder();

            _process = new Process();
            _process.StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "dotnet",
                Arguments = path
            };
            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += Process_OutputDataReceived;
            _process.ErrorDataReceived += Process_ErrorDataReceived;
            _process.Start();

            _processEx = new ProcessEx(output, _process, Timeout.InfiniteTimeSpan);

            _startTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public string GetOutput()
        {
            lock (_outputLock)
            {
                return _output.ToString();
            }
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
                var m = NowListeningRegex.Match(data);
                if (m.Success)
                {
                    ServerPort = m.Groups["port"].Value;
                }

                if (data.Contains("Application started. Press Ctrl+C to shut down."))
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
