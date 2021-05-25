// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Internal
{
    internal class ProcessEx : IDisposable
    {
        private static readonly TimeSpan DefaultProcessTimeout = TimeSpan.FromHours(5);
        private static readonly string NUGET_PACKAGES = GetNugetPackagesRestorePath();

        private readonly ITestOutputHelper _output;
        private readonly Process _process;
        private readonly StringBuilder _stderrCapture;
        private readonly StringBuilder _stdoutCapture;
        private readonly object _pipeCaptureLock = new object();
        private readonly object _testOutputLock = new object();
        private BlockingCollection<string> _stdoutLines;
        private readonly TaskCompletionSource<int> _exited;
        private readonly CancellationTokenSource _stdoutLinesCancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        private readonly CancellationTokenSource _processTimeoutCts;
        private bool _disposed;

        public ProcessEx(ITestOutputHelper output, Process proc, TimeSpan timeout)
        {
            _output = output;
            _stdoutCapture = new StringBuilder();
            _stderrCapture = new StringBuilder();
            _stdoutLines = new BlockingCollection<string>();
            _exited = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            _process = proc;
            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += OnOutputData;
            proc.ErrorDataReceived += OnErrorData;
            proc.Exited += OnProcessExited;
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();


            // We greedily create a timeout exception message even though a timeout is unlikely to happen for two reasons:
            // 1. To make it less likely for Process getters to throw exceptions like "System.InvalidOperationException: Process has exited, ..."
            // 2. To ensure if/when exceptions are thrown from Process getters, these exceptions can easily be observed.
            var timeoutExMessage = $"Process proc {proc.ProcessName} {proc.StartInfo.Arguments} timed out after {timeout}.";

            _processTimeoutCts = new CancellationTokenSource(timeout);
            _processTimeoutCts.Token.Register(() =>
            {
                _exited.TrySetException(new TimeoutException(timeoutExMessage));
            });
        }

        public Process Process => _process;

        public Task Exited => _exited.Task;

        public bool HasExited => _process.HasExited;

        public string Error
        {
            get
            {
                lock (_pipeCaptureLock)
                {
                    return _stderrCapture.ToString();
                }
            }
        }

        public string Output
        {
            get
            {
                lock (_pipeCaptureLock)
                {
                    return _stdoutCapture.ToString();
                }
            }
        }

        public IEnumerable<string> OutputLinesAsEnumerable => _stdoutLines.GetConsumingEnumerable(_stdoutLinesCancellationSource.Token);

        public int ExitCode => _process.ExitCode;

        public object Id => _process.Id;

        public static ProcessEx Run(ITestOutputHelper output, string workingDirectory, string command, string args = null, IDictionary<string, string> envVars = null, TimeSpan? timeout = default)
        {
            var startInfo = new ProcessStartInfo(command, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            if (envVars != null)
            {
                foreach (var envVar in envVars)
                {
                    startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }
            }

            startInfo.EnvironmentVariables["NUGET_PACKAGES"] = NUGET_PACKAGES;

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("helix")))
            {
                startInfo.EnvironmentVariables["NUGET_FALLBACK_PACKAGES"] = Environment.GetEnvironmentVariable("NUGET_FALLBACK_PACKAGES");
            }

            output.WriteLine($"==> {startInfo.FileName} {startInfo.Arguments} [{startInfo.WorkingDirectory}]");
            var proc = Process.Start(startInfo);

            return new ProcessEx(output, proc, timeout ?? DefaultProcessTimeout);
        }

        private void OnErrorData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            lock (_pipeCaptureLock)
            {
                _stderrCapture.AppendLine(e.Data);
            }

            lock (_testOutputLock)
            {
                if (!_disposed)
                {
                    _output.WriteLine("[ERROR] " + e.Data);
                }
            }
        }

        private void OnOutputData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            lock (_pipeCaptureLock)
            {
                _stdoutCapture.AppendLine(e.Data);
            }

            lock (_testOutputLock)
            {
                if (!_disposed)
                {
                    _output.WriteLine(e.Data);
                }
            }

            if (_stdoutLines != null)
            {
                _stdoutLines.Add(e.Data);
            }
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            lock (_testOutputLock)
            {
                if (!_disposed)
                {
                    _output.WriteLine("Process exited.");
                }
            }
            _process.WaitForExit();
            _stdoutLines.CompleteAdding();
            _stdoutLines = null;
            _exited.TrySetResult(_process.ExitCode);
        }

        internal string GetFormattedOutput()
        {
            if (!_process.HasExited)
            {
                throw new InvalidOperationException($"Process {_process.ProcessName} with pid: {_process.Id} has not finished running.");
            }

            return $"Process exited with code {_process.ExitCode}\nStdErr: {Error}\nStdOut: {Output}";
        }

        public void WaitForExit(bool assertSuccess, TimeSpan? timeSpan = null)
        {
            if(!timeSpan.HasValue)
            {
                timeSpan = TimeSpan.FromSeconds(600);
            }

            var exited = Exited.Wait(timeSpan.Value);
            if (!exited)
            {
                lock (_testOutputLock)
                {
                    _output.WriteLine($"The process didn't exit within the allotted time ({timeSpan.Value.TotalSeconds} seconds).");
                }

                _process.Dispose();
            }
            else if (assertSuccess && _process.ExitCode != 0)
            {
                throw new Exception($"Process exited with code {_process.ExitCode}\nStdErr: {Error}\nStdOut: {Output}");
            }
        }

        private static string GetNugetPackagesRestorePath() => (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NUGET_RESTORE")))
            ? typeof(ProcessEx).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "TestPackageRestorePath")
                ?.Value
            : Environment.GetEnvironmentVariable("NUGET_RESTORE");

        public void Dispose()
        {
            _processTimeoutCts.Dispose();

            lock (_testOutputLock)
            {
                _disposed = true;
            }

            if (_process != null && !_process.HasExited)
            {
                _process.KillTree();
            }

            if (_process != null)
            {
                _process.CancelOutputRead();
                _process.CancelErrorRead();

                _process.ErrorDataReceived -= OnErrorData;
                _process.OutputDataReceived -= OnOutputData;
                _process.Exited -= OnProcessExited;
                _process.Dispose();
            }
        }
    }
}
