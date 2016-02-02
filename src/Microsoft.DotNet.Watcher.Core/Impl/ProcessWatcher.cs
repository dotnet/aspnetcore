// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.DotNet.Watcher.Core
{
    public class ProcessWatcher : IProcessWatcher
    {
        private static readonly bool _isWindows = PlatformServices.Default.Runtime.OperatingSystem.Equals("Windows", StringComparison.OrdinalIgnoreCase);

        private Process _runningProcess;

        public int Start(string executable, string arguments, string workingDir)
        {
            // This is not thread safe but it will not run in a multithreaded environment so don't worry
            if (_runningProcess != null)
            {
                throw new InvalidOperationException("The previous process is still running");
            }

            _runningProcess = new Process();
            _runningProcess.StartInfo = new ProcessStartInfo()
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = workingDir
            };

            _runningProcess.Start();

            return _runningProcess.Id;
        }

        public Task<int> WaitForExitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => KillProcess(_runningProcess?.Id));

            return Task.Run(() =>
            {
                _runningProcess.WaitForExit();

                var exitCode = _runningProcess.ExitCode;
                _runningProcess = null;

                return exitCode;
            });
        }

        private void KillProcess(int? processId)
        {
            if (processId == null)
            {
                return;
            }

            ProcessStartInfo startInfo;

            if (_isWindows)
            {
                startInfo = new ProcessStartInfo()
                {
                    FileName = "taskkill",
                    Arguments = $"/T /F /PID {processId}",
                };
            }
            else
            {
                startInfo = new ProcessStartInfo()
                {
                    FileName = "pkill",
                    Arguments = $"-TERM -P {processId}",
                };
            }
            var killProcess =  Process.Start(startInfo);
            killProcess.WaitForExit();
        }
    }
}