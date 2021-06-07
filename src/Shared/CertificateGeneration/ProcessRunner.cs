using System;
using System.Diagnostics;
using System.IO;
using System.Text;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class ProcessRunner
    {
        public static ProcessRunResult Run(ProcessRunOptions options)
        {
            Process process = new Process()
            {
                StartInfo =
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true
                }
            };
            int i = 0;
            if (options.Elevate)
            {
                process.StartInfo.FileName = "sudo";
            }
            else
            {
                process.StartInfo.FileName = options.Command[i++];
            }
            for (; i < options.Command.Count; i++)
            {
                process.StartInfo.ArgumentList.Add(options.Command[i]);
            }
            string commandLine = $"{process.StartInfo.FileName} {string.Join(" ", process.StartInfo.ArgumentList)}";
            bool readStdErr = options.ThrowOnFailure || options.ReadStandardError;
            StringBuilder? stdErr = null;
            if (readStdErr)
            {
                stdErr = new StringBuilder();
                process.ErrorDataReceived += (o, e) => {
                    if (e.Data != null)
                    {
                        stdErr.AppendLine(e.Data);
                    }
                };
            }
            StringBuilder? stdOut = null;
            bool readStdOut = options.ReadStandardOutput;
            if (readStdOut)
            {
                stdOut = new StringBuilder();
                process.OutputDataReceived += (o, e) => {
                    if (e.Data != null)
                    {
                        stdOut.AppendLine(e.Data);
                    }
                };
            }
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.StandardInput.Close();
            process.WaitForExit();
            if (options.ThrowOnFailure && process.ExitCode != 0)
            {
                throw new Exception($"Command '{commandLine}' failed with {process.ExitCode}: {stdErr}.");
            }
            return new ProcessRunResult
            {
                ExitCode = process.ExitCode,
                CommandLine = commandLine,
                StandardError = stdErr,
                StandardOutput = stdOut
            };
        }

        public static bool HasProgram(string program)
        {
            string path;
            string? pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                string[] pathItems = pathEnvVar.Split(':', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pathItem in pathItems)
                {
                    path = Path.Combine(pathItem, program);
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}