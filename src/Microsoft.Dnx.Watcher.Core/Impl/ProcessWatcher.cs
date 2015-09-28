// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Dnx.Watcher.Core
{
    public class ProcessWatcher : IProcessWatcher
    {
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

            RemoveCompilationPortEnvironmentVariable(_runningProcess.StartInfo);

            _runningProcess.Start();

            return _runningProcess.Id;
        }

        public Task<int> WaitForExitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _runningProcess?.Kill());
            
            return Task.Run(() => 
            {
                _runningProcess.WaitForExit();

                var exitCode = _runningProcess.ExitCode;
                _runningProcess = null;

                return exitCode;
            });
        }

        private static void RemoveCompilationPortEnvironmentVariable(ProcessStartInfo procStartInfo)
        {
            string[] _environmentVariablesToRemove = new string[]
            {
                "DNX_COMPILATION_SERVER_PORT",
            };

#if DNX451
            var environmentVariables = procStartInfo.EnvironmentVariables.Keys.Cast<string>();
#else
            var environmentVariables = procStartInfo.Environment.Keys;
#endif

            var envVarsToRemove = environmentVariables
                .Where(envVar => _environmentVariablesToRemove.Contains(envVar, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            // Workaround for the DNX start issue (it passes some environment variables that it shouldn't)
            foreach (var envVar in envVarsToRemove)
            {
#if DNX451
                procStartInfo.EnvironmentVariables.Remove(envVar);
#else
                procStartInfo.Environment.Remove(envVar);
#endif
            }
        }
    }
}