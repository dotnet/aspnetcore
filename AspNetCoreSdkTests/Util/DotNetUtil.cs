using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AspNetCoreSdkTests.Util
{
    internal static class DotNetUtil
    {
        // Bind to dynamic port 0 to avoid port conflicts during parallel tests
        private const string _urls = "--urls http://127.0.0.1:0;https://127.0.0.1:0";

        public static string PublishOutput => "pub";

        private static IEnumerable<KeyValuePair<string, string>> GetEnvironment(string workingDirectory)
        {
            // Set NUGET_PACKAGES to an empty folder to ensure all packages are loaded from either NuGetFallbackFolder or configured sources,
            // and *not* loaded from the default per-user global-packages folder.
            yield return new KeyValuePair<string, string>("NUGET_PACKAGES", Path.Combine(workingDirectory, ".nuget", "packages"));
        }

        public static string New(string template, string workingDirectory)
        {
            return RunDotNet($"new {template} --name {template} --output . --no-restore", workingDirectory, GetEnvironment(workingDirectory));
        }

        public static string Restore(string workingDirectory, NuGetConfig config)
        {
            var configPath = Path.GetFullPath(Path.Combine("NuGetConfig", $"NuGet.{config}.config"));
            return RunDotNet($"restore --no-cache --configfile {configPath}", workingDirectory, GetEnvironment(workingDirectory));
        }

        public static string Build(string workingDirectory)
        {
            return RunDotNet("build --no-restore", workingDirectory, GetEnvironment(workingDirectory));
        }

        public static (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) Run(string workingDirectory)
        {
            return StartDotNet($"run --no-build {_urls}", workingDirectory, GetEnvironment(workingDirectory));
        }

        internal static (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) Exec(string workingDirectory, string name)
        {
            var path = Path.Combine(PublishOutput, $"{name}.dll");
            return StartDotNet($"exec {path} {_urls}", workingDirectory, GetEnvironment(workingDirectory));
        }

        public static string Publish(string workingDirectory)
        {
            return RunDotNet($"publish --no-build -o {PublishOutput}", workingDirectory, GetEnvironment(workingDirectory));
        }

        private static string RunDotNet(string arguments, string workingDirectory,
            IEnumerable<KeyValuePair<string, string>> environment = null, bool throwOnError = true)
        {
            var p = StartDotNet(arguments, workingDirectory, environment);
            return WaitForExit(p.Process, p.OutputBuilder, p.ErrorBuilder, throwOnError: throwOnError);
        }

        private static (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) StartDotNet(
            string arguments, string workingDirectory, IEnumerable<KeyValuePair<string, string>> environment = null)
        {
            return StartProcess("dotnet", arguments, workingDirectory, environment);
        }

        private static (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) StartProcess(
            string filename, string arguments, string workingDirectory, IEnumerable<KeyValuePair<string, string>> environment = null)
        {
            var process = new Process()
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                },
            };

            if (environment != null)
            {
                foreach (var kvp in environment)
                {
                    process.StartInfo.Environment.Add(kvp);
                }
            }

            var outputBuilder = new ConcurrentStringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                outputBuilder.AppendLine(e.Data);
            };

            var errorBuilder = new ConcurrentStringBuilder();
            process.ErrorDataReceived += (_, e) =>
            {
                errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return (process, outputBuilder, errorBuilder);
        }

        public static string StopProcess(Process process, ConcurrentStringBuilder outputBuilder, ConcurrentStringBuilder errorBuilder,
            bool throwOnError = true)
        {
            if (!process.HasExited)
            {
                process.KillTree();
            }

            return WaitForExit(process, outputBuilder, errorBuilder, throwOnError: throwOnError);
        }

        public static string WaitForExit(Process process, ConcurrentStringBuilder outputBuilder, ConcurrentStringBuilder errorBuilder,
            bool throwOnError = true)
        {
            // Workaround issue where WaitForExit() blocks until child processes are killed, which is problematic
            // for the dotnet.exe NodeReuse child processes.  I'm not sure why this is problematic for dotnet.exe child processes
            // but not for MSBuild.exe child processes.  The workaround is to specify a large timeout.
            // https://stackoverflow.com/a/37983587/102052
            process.WaitForExit(int.MaxValue);

            if (throwOnError && process.ExitCode != 0)
            {
                var sb = new ConcurrentStringBuilder();

                sb.AppendLine($"Command {process.StartInfo.FileName} {process.StartInfo.Arguments} returned exit code {process.ExitCode}");
                sb.AppendLine();
                sb.AppendLine(outputBuilder.ToString());

                throw new InvalidOperationException(sb.ToString());
            }

            return outputBuilder.ToString();
        }
    }
}
