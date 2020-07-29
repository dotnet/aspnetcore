using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessUtilities
{
    public static class ProcessUtil
    {
        [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
        private static extern int sys_kill(int pid, int sig);

        public static Task CaptureDumpAsync()
        {
            var dumpDirectoryPath = Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");

            if (dumpDirectoryPath == null)
            {
                return Task.CompletedTask;
            }

            var process = Process.GetCurrentProcess();
            var dumpFilePath = Path.Combine(dumpDirectoryPath, $"{process.ProcessName}-{process.Id}.dmp");

            return CaptureDumpAsync(process.Id, dumpFilePath);
        }

        public static Task CaptureDumpAsync(int pid)
        {
            var dumpDirectoryPath = Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");

            if (dumpDirectoryPath == null)
            {
                return Task.CompletedTask;
            }

            var process = Process.GetProcessById(pid);
            var dumpFilePath = Path.Combine(dumpDirectoryPath, $"{process.ProcessName}.{process.Id}.dmp");

            return CaptureDumpAsync(process.Id, dumpFilePath);
        }

        public static Task CaptureDumpAsync(int pid, string dumpFilePath)
        {
            // Skip this on OSX, we know it's unsupported right now
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Can we capture stacks or do a gcdump instead?
                return Task.CompletedTask;
            }

            return RunAsync("dotnet-dump", $"collect -p {pid} -o \"{dumpFilePath}\"");
        }

        public static async Task<(string, string, int)> RunAsync(
            string filename,
            string arguments,
            string workingDirectory = null,
            string dumpDirectoryPath = null,
            bool throwOnError = true,
            IDictionary<string, string> environmentVariables = null,
            Action<string> outputDataReceived = null,
            Action<string> errorDataReceived = null,
            Action<int> onStart = null,
            CancellationToken cancellationToken = default)
        {
            using var process = new Process()
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };


            if (workingDirectory != null)
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            dumpDirectoryPath ??= Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");

            if (dumpDirectoryPath != null)
            {
                process.StartInfo.EnvironmentVariables["COMPlus_DbgEnableMiniDump"] = "1";
                process.StartInfo.EnvironmentVariables["COMPlus_DbgMiniDumpName"] = Path.Combine(dumpDirectoryPath, $"{Path.GetFileName(filename)}.%d.dmp");
            }

            if (environmentVariables != null)
            {
                foreach (var kvp in environmentVariables)
                {
                    process.StartInfo.Environment.Add(kvp);
                }
            }

            var outputBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    if (outputDataReceived != null)
                    {
                        outputDataReceived.Invoke(e.Data);
                    }
                    else
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                }
            };

            var errorBuilder = new StringBuilder();
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    if (errorDataReceived != null)
                    {
                        errorDataReceived.Invoke(e.Data);
                    }
                    else
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                }
            };

            var processLifetimeTask = new TaskCompletionSource<(string, string, int)>();

            process.Exited += (_, e) =>
            {
                if (throwOnError && process.ExitCode != 0)
                {
                    processLifetimeTask.TrySetException(new InvalidOperationException($"Command {filename} {arguments} returned exit code {process.ExitCode}. \r\nStdOut:{outputBuilder}, \r\nStdErr: {errorBuilder}"));
                }
                else
                {
                    processLifetimeTask.TrySetResult((outputBuilder.ToString(), errorBuilder.ToString(), process.ExitCode));
                }
            };

            process.Start();
            onStart?.Invoke(process.Id);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var cancelledTcs = new TaskCompletionSource<object>();
            await using var _ = cancellationToken.Register(() => cancelledTcs.TrySetResult(null));

            var result = await Task.WhenAny(processLifetimeTask.Task, cancelledTcs.Task);

            if (result == cancelledTcs.Task)
            {
                if (dumpDirectoryPath != null)
                {
                    var dumpFilePath = Path.Combine(dumpDirectoryPath, $"{Path.GetFileName(filename)}.{process.Id}.dmp");
                    // Capture a process dump if the dumpDirectory is set
                    await CaptureDumpAsync(process.Id, dumpFilePath);
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    sys_kill(process.Id, sig: 2); // SIGINT

                    var cancel = new CancellationTokenSource();

                    await Task.WhenAny(processLifetimeTask.Task, Task.Delay(TimeSpan.FromSeconds(5), cancel.Token));

                    cancel.Cancel();
                }

                if (!process.HasExited)
                {
                    process.CloseMainWindow();

                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }

            return await processLifetimeTask.Task;
        }
    }
}
