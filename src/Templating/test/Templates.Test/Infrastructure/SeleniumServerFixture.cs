// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Templates.Test.Helpers;

namespace Templates.Test.Infrastructure
{
    public class SeleniumServerFixture : IDisposable
    {
        private string _workingDirectory;
        private Process _serverProcess;
        private static readonly object _serverLock = new object();
        private const int ProcessTimeoutMilliseconds = 30 * 1000;

        public SeleniumServerFixture()
        {
            _workingDirectory = Directory.GetCurrentDirectory();
            StartSeleniumStandaloneServer();
        }

        public void Dispose()
        {
            if(_serverProcess != null)
            {
                _serverProcess.KillTree();
                _serverProcess.Dispose();
            }
        }

        public void StartSeleniumStandaloneServer()
        {
            if (WebDriverFactory.HostSupportsBrowserAutomation)
            {
                lock (_serverLock)
                {
                    // We have to make node_modules in this folder so that it doesn't go hunting higher up the tree
                    RunViaShell(_workingDirectory, "mkdir node_modules").WaitForExit();
                    var npmInstallProcess = RunViaShell(_workingDirectory, $"npm install --prefix {_workingDirectory} selenium-standalone@6.15.1");
                    npmInstallProcess.WaitForExit();

                    if (npmInstallProcess.ExitCode != 0)
                    {
                        var output = npmInstallProcess.StandardOutput.ReadToEnd();
                        var error = npmInstallProcess.StandardError.ReadToEnd();
                        throw new Exception($"Npm install exited with code {npmInstallProcess.ExitCode}\nStdErr: {error}\nStdOut: {output}");
                    }
                    npmInstallProcess.KillTree();
                    npmInstallProcess.Dispose();
                }
                lock (_serverLock)
                {
                    var seleniumInstallProcess = RunViaShell(_workingDirectory, "npx selenium-standalone install");
                    seleniumInstallProcess.WaitForExit(ProcessTimeoutMilliseconds);
                    if (seleniumInstallProcess.ExitCode != 0)
                    {
                        var output = seleniumInstallProcess.StandardOutput.ReadToEnd();
                        var error = seleniumInstallProcess.StandardError.ReadToEnd();
                        throw new Exception($"selenium install exited with code {seleniumInstallProcess.ExitCode}\nStdErr: {error}\nStdOut: {output}");
                    }
                    seleniumInstallProcess.KillTree();
                    seleniumInstallProcess.Dispose();
                }

                // Starts a process that runs the selenium server
                _serverProcess = RunViaShell(_workingDirectory, "npx selenium-standalone start");
                string line = "";
                while (line != null && !line.StartsWith("Selenium started") && !_serverProcess.StandardOutput.EndOfStream)
                {
                    line = _serverProcess.StandardOutput.ReadLine();
                }
            }
        }

        private static Process RunViaShell(string workingDirectory, string commandAndArgs)
        {
            var (shellExe, argsPrefix) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ("cmd", "/c")
                : ("bash", "-c");

            return Run(workingDirectory, shellExe, $"{argsPrefix} \"{commandAndArgs}\"");
        }

        private static Process Run(string workingDirectory, string command, string args = null, IDictionary<string, string> envVars = null)
        {
            var startInfo = new ProcessStartInfo(command, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            return Process.Start(startInfo);
        }
    }
}
