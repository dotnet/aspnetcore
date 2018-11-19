// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public class TestCommand
    {
        private List<string> _cliGeneratedEnvironmentVariables = new List<string> { "MSBuildSDKsPath" };

        public string Command { get; }

        public Process CurrentProcess { get; private set; }

        public Dictionary<string, string> Environment { get; } = new Dictionary<string, string>();

        public event DataReceivedEventHandler ErrorDataReceived;

        public event DataReceivedEventHandler OutputDataReceived;

        public string WorkingDirectory { get; set; }
        public ILogger Logger { get; set; }

        public TestCommand(string command)
        {
            Command = command;
        }

        public void KillTree()
        {
            if (CurrentProcess == null)
            {
                throw new InvalidOperationException("No process is available to be killed");
            }

            CurrentProcess.KillTree();
        }

        public virtual async Task<CommandResult> ExecuteAsync(string args = "")
        {
            var resolvedCommand = Command;

            Logger.LogDebug($"Executing - {resolvedCommand} {args} - {WorkingDirectoryInfo()}");

            return await ExecuteAsyncInternal(resolvedCommand, args);
        }

        public virtual async Task<CommandResult> ExecuteAndAssertAsync(string args = "")
        {
            var result = await ExecuteAsync(args);
            result.AssertSuccess();
            return result;
        }

        private async Task<CommandResult> ExecuteAsyncInternal(string executable, string args)
        {
            var stdOut = new List<String>();

            var stdErr = new List<String>();

            CurrentProcess = CreateProcess(executable, args);

            CurrentProcess.ErrorDataReceived += (s, e) =>
            {
                stdErr.Add(e.Data);

                var handler = ErrorDataReceived;

                if (handler != null)
                {
                    handler(s, e);
                }
            };

            CurrentProcess.OutputDataReceived += (s, e) =>
            {
                stdOut.Add(e.Data);

                var handler = OutputDataReceived;

                if (handler != null)
                {
                    handler(s, e);
                }
            };

            var completionTask = StartAndWaitForExitAsync(CurrentProcess);

            CurrentProcess.BeginOutputReadLine();

            CurrentProcess.BeginErrorReadLine();

            await completionTask;

            CurrentProcess.WaitForExit();

            RemoveNullTerminator(stdOut);

            RemoveNullTerminator(stdErr);

            var stdOutString = String.Join(System.Environment.NewLine, stdOut);
            var stdErrString = String.Join(System.Environment.NewLine, stdErr);

            if (!string.IsNullOrWhiteSpace(stdOutString))
            {
                Logger.LogDebug("stdout: {out}", stdOutString);
            }

            if (!string.IsNullOrWhiteSpace(stdErrString))
            {
                Logger.LogDebug("stderr: {err}", stdErrString);
            }

            return new CommandResult(
                CurrentProcess.StartInfo,
                CurrentProcess.ExitCode,
                stdOutString,
                stdErrString);
        }

        private Process CreateProcess(string executable, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            RemoveCliGeneratedEnvironmentVariablesFrom(psi);

            AddEnvironmentVariablesTo(psi);

            AddWorkingDirectoryTo(psi);

            var process = new Process
            {
                StartInfo = psi
            };

            process.EnableRaisingEvents = true;

            return process;
        }

        private string WorkingDirectoryInfo()
        {
            if (WorkingDirectory == null)
            {
                return "";
            }

            return $" in {WorkingDirectory}";
        }

        private void RemoveNullTerminator(List<string> strings)
        {
            var count = strings.Count;

            if (count < 1)
            {
                return;
            }

            if (strings[count - 1] == null)
            {
                strings.RemoveAt(count - 1);
            }
        }

        private void RemoveCliGeneratedEnvironmentVariablesFrom(ProcessStartInfo psi)
        {
            foreach (var name in _cliGeneratedEnvironmentVariables)
            {
                psi.Environment.Remove(name);
            }
        }

        private void AddEnvironmentVariablesTo(ProcessStartInfo psi)
        {
            foreach (var item in Environment)
            {
                psi.Environment[item.Key] = item.Value;
            }
        }

        private void AddWorkingDirectoryTo(ProcessStartInfo psi)
        {
            if (!string.IsNullOrWhiteSpace(WorkingDirectory))
            {
                psi.WorkingDirectory = WorkingDirectory;
            }
        }
        public static Task StartAndWaitForExitAsync(Process subject)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            subject.EnableRaisingEvents = true;

            subject.Exited += (s, a) =>
            {
                taskCompletionSource.SetResult(null);
            };

            subject.Start();

            return taskCompletionSource.Task;
        }
    }
}
