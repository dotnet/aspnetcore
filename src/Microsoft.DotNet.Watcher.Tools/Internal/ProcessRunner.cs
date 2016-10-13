// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class ProcessRunner
    {
        private readonly ILogger _logger;

        public ProcessRunner(ILogger logger)
        {
            Ensure.NotNull(logger, nameof(logger));

            _logger = logger;
        }

        // May not be necessary in the future. See https://github.com/dotnet/corefx/issues/12039
        public async Task<int> RunAsync(ProcessSpec processSpec, CancellationToken cancellationToken)
        {
            Ensure.NotNull(processSpec, nameof(processSpec));

            int exitCode;

            using (var process = CreateProcess(processSpec))
            using (var processState = new ProcessState(process))
            {
                cancellationToken.Register(() => processState.TryKill());

                process.Start();
                _logger.LogInformation("{execName} process id: {pid}", processSpec.ShortDisplayName(), process.Id);

                await processState.Task;

                exitCode = process.ExitCode;
            }

            LogResult(processSpec, exitCode);

            return exitCode;
        }

        private Process CreateProcess(ProcessSpec processSpec)
        {
            var arguments = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(processSpec.Arguments);

            _logger.LogInformation("Running {execName} with the following arguments: {args}", processSpec.ShortDisplayName(), arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = processSpec.Executable,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = processSpec.WorkingDirectory
            };
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            return process;
        }

        private void LogResult(ProcessSpec processSpec, int exitCode)
        {
            var processName = processSpec.ShortDisplayName();
            if (exitCode == 0)
            {
                _logger.LogInformation("{execName} exit code: {code}", processName, exitCode);
            }
            else
            {
                _logger.LogError("{execName} exit code: {code}", processName, exitCode);
            }
        }

        private class ProcessState : IDisposable
        {
            private readonly Process _process;
            private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
            private volatile bool _disposed;

            public ProcessState(Process process)
            {
                _process = process;
                _process.Exited += OnExited;
            }

            public Task Task => _tcs.Task;

            public void TryKill()
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.KillTree();
                    }
                }
                catch
                { }
            }

            private void OnExited(object sender, EventArgs args)
                => _tcs.TrySetResult(null);

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    TryKill();
                    _process.Exited -= OnExited;
                    _process.Dispose();
                }
            }
        }
    }
}