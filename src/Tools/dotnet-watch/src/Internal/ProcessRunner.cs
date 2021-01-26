// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class ProcessRunner
    {
        private readonly IReporter _reporter;

        public ProcessRunner(IReporter reporter)
        {
            Ensure.NotNull(reporter, nameof(reporter));

            _reporter = reporter;
        }

        // May not be necessary in the future. See https://github.com/dotnet/corefx/issues/12039
        public async Task<int> RunAsync(ProcessSpec processSpec, CancellationToken cancellationToken)
        {
            Ensure.NotNull(processSpec, nameof(processSpec));

            int exitCode;

            var stopwatch = new Stopwatch();

            using (var process = CreateProcess(processSpec))
            using (var processState = new ProcessState(process, _reporter))
            {
                cancellationToken.Register(() => processState.TryKill());

                var readOutput = false;
                var readError = false;
                if (processSpec.IsOutputCaptured)
                {
                    readOutput = true;
                    readError = true;
                    process.OutputDataReceived += (_, a) =>
                    {
                        if (!string.IsNullOrEmpty(a.Data))
                        {
                            processSpec.OutputCapture.AddLine(a.Data);
                        }
                    };
                    process.ErrorDataReceived += (_, a) =>
                    {
                        if (!string.IsNullOrEmpty(a.Data))
                        {
                            processSpec.OutputCapture.AddLine(a.Data);
                        }
                    };
                }
                else if (processSpec.OnOutput != null)
                {
                    readOutput = true;
                    process.OutputDataReceived += processSpec.OnOutput;
                }

                stopwatch.Start();
                process.Start();

                _reporter.Verbose($"Started '{processSpec.Executable}' with process id {process.Id}");

                if (readOutput)
                {
                    process.BeginOutputReadLine();
                }
                if (readError)
                {
                    process.BeginErrorReadLine();
                }

                await processState.Task;

                exitCode = process.ExitCode;
                stopwatch.Stop();
                _reporter.Verbose($"Process id {process.Id} ran for {stopwatch.ElapsedMilliseconds}ms");
            }

            return exitCode;
        }

        private Process CreateProcess(ProcessSpec processSpec)
        {
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = processSpec.Executable,
                    Arguments = ArgumentEscaper.EscapeAndConcatenate(processSpec.Arguments),
                    UseShellExecute = false,
                    WorkingDirectory = processSpec.WorkingDirectory,
                    RedirectStandardOutput = processSpec.IsOutputCaptured || (processSpec.OnOutput != null),
                    RedirectStandardError = processSpec.IsOutputCaptured,
                }
            };

            foreach (var env in processSpec.EnvironmentVariables)
            {
                process.StartInfo.Environment.Add(env.Key, env.Value);
            }

            return process;
        }

        private class ProcessState : IDisposable
        {
            private readonly IReporter _reporter;
            private readonly Process _process;
            private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
            private volatile bool _disposed;

            public ProcessState(Process process, IReporter reporter)
            {
                _reporter = reporter;
                _process = process;
                _process.Exited += OnExited;
                Task = _tcs.Task.ContinueWith(_ =>
                {
                    try
                    {
                        // We need to use two WaitForExit calls to ensure that all of the output/events are processed. Previously
                        // this code used Process.Exited, which could result in us missing some output due to the ordering of
                        // events.
                        //
                        // See the remarks here: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit#System_Diagnostics_Process_WaitForExit_System_Int32_
                        if (!_process.WaitForExit(Int32.MaxValue))
                        {
                            throw new TimeoutException();
                        }

                        _process.WaitForExit();
                    }
                    catch (InvalidOperationException)
                    {
                        // suppress if this throws if no process is associated with this object anymore.
                    }
                });
            }

            public Task Task { get; }

            public void TryKill()
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    if (!_process.HasExited)
                    {
                        _reporter.Verbose($"Killing process {_process.Id}");
                        _process.KillTree();
                    }
                }
                catch (Exception ex)
                {
                    _reporter.Verbose($"Error while killing process '{_process.StartInfo.FileName} {_process.StartInfo.Arguments}': {ex.Message}");
#if DEBUG
                    _reporter.Verbose(ex.ToString());
#endif
                }
            }

            private void OnExited(object sender, EventArgs args)
                => _tcs.TrySetResult(null);

            public void Dispose()
            {
                if (!_disposed)
                {
                    TryKill();
                    _disposed = true;
                    _process.Exited -= OnExited;
                    _process.Dispose();
                }
            }
        }
    }
}
