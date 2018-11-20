// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Templates.Test
{
    internal static class ProcessManager
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(150);

        public static Task<ProcessResult> RunProcessAsync(ProcessStartInfo processStartInfo)
        {
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardOutput = true;

            var process = new Process()
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true,
            };

            var output = new StringBuilder();
            var outputLock = new object();

            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.OutputDataReceived += Process_OutputDataReceived;

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            var timeoutTask = Task.Delay(Timeout).ContinueWith((t) =>
            {
                // Don't timeout during debug sessions
                while (Debugger.IsAttached)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                if (process.HasExited)
                {
                    // This will happen on success, the 'real' task has already completed so this value will
                    // never be visible.
                    return (ProcessResult)default;
                }

                // This is a timeout.
                process.Kill();
                throw new TimeoutException($"command '${process.StartInfo.FileName} {process.StartInfo.Arguments}' timed out after {Timeout}.");
            });

            var waitTask = Task.Run(() =>
            {
                // We need to use two WaitForExit calls to ensure that all of the output/events are processed. Previously
                // this code used Process.Exited, which could result in us missing some output due to the ordering of
                // events.
                //
                // See the remarks here: https://msdn.microsoft.com/en-us/library/ty0d8k56(v=vs.110).aspx
                if (!process.WaitForExit(int.MaxValue))
                {
                    // unreachable - the timeoutTask will kill the process before this happens.
                    throw new TimeoutException();
                }

                process.WaitForExit();

                string outputString;
                lock (outputLock)
                {
                    outputString = output.ToString();
                }

                return new ProcessResult(processStartInfo, process.ExitCode, outputString);
            });

            return Task.WhenAny<ProcessResult>(waitTask, timeoutTask).Unwrap();

            void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                lock (outputLock)
                {
                    output.AppendLine(e.Data);
                }
            }

            void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                lock (outputLock)
                {
                    output.AppendLine(e.Data);
                }
            }
        }
    }
}
