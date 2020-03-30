using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace app
{
    class Program
    {
        static void Main(string[] args)
        {
            var target = Environment.GetEnvironmentVariable("ASPNETCORE_TEST_TARGET");
            var sdkVersion = Environment.GetEnvironmentVariable("ASPNETCORE_SDK_VERSION");
            var runtimeVersion = Environment.GetEnvironmentVariable("ASPNETCORE_RUNTIME_VERSION");
            var helixQueue = Environment.GetEnvironmentVariable("ASPNETCORE_HELIX_QUEUE");
            var architecture = Environment.GetEnvironmentVariable("ASPNETCORE_ARCHITECTURE");
            var quarantined = Environment.GetEnvironmentVariable("ASPNETCORE_QUARANTINED");
            var efVersion = Environment.GetEnvironmentVariable("ASPNETCORE_EF_VERSION");
            var HELIX_WORKITEM_ROOT = Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT");

            var path = Environment.GetEnvironmentVariable("PATH");
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            Console.WriteLine("Checking for Microsoft.AspNetCore.App");
            if (Directory.Exists("Microsoft.AspNetCore.App"))
            {
                Console.WriteLine($"Found Microsoft.AspNetCore.App, copying to {dotnetRoot}/shared/Microsoft.AspNetCore.App/{runtimeVersion}");
                foreach (var file in Directory.EnumerateFiles("Microsoft.AspNetCore.App", "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(file, $"{dotnetRoot}/shared/Microsoft.AspNetCore.App/{runtimeVersion}", overwrite: true);
                }

                Console.WriteLine($"Adding current directory to nuget sources: {HELIX_WORKITEM_ROOT}");

                var processInfo = new ProcessStartInfo()
                {
                    Arguments = $"nuget add source {HELIX_WORKITEM_ROOT}",
                    FileName = $"{dotnetRoot}/dotnet",
                };

                processInfo.Environment["PATH"] = path;

                var process = Process.Start(processInfo);
                process.WaitForExit();

                processInfo.Arguments = "nuget add source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json";
                process = Process.Start(processInfo);
                process.WaitForExit();

                processInfo.Arguments = "nuget list source";
                process = Process.Start(processInfo);
                process.WaitForExit();

                processInfo.Arguments = $"tool install dotnet-ef --global --version {efVersion}";
                process = Process.Start(processInfo);
                process.WaitForExit();

                path += $";{Environment.GetEnvironmentVariable("DOTNET_CLI_HOME")}/.dotnet/tools";
            }


            var testProcessInfo = new ProcessStartInfo()
            {
                Arguments = $"vstest {target} -lt",
                FileName = $"{dotnetRoot}/dotnet",
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            testProcessInfo.Environment["DOTNET_ROOT"] = dotnetRoot;
            testProcessInfo.Environment["PATH"] = path;

            testProcessInfo.Environment["HELIX"] = helixQueue;
            Console.WriteLine($"Current Directory: {HELIX_WORKITEM_ROOT}");
            var helixDir = Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT");
            Console.WriteLine($"Setting HELIX_DIR: {helixDir}");
            testProcessInfo.Environment["HELIX_DIR"] = helixDir;
            testProcessInfo.Environment["NUGET_FALLBACK_PACKAGES"] = helixDir;
            var nugetRestore = Path.Combine(helixDir, "nugetRestore");
            Console.WriteLine($"Creating nuget restore directory: {nugetRestore}");
            testProcessInfo.Environment["NUGET_RESTORE"] = nugetRestore;
            var dotnetEFFullPath = Path.Combine(nugetRestore, $"dotnet-ef/{efVersion}/tools/netcoreapp3.1/any/dotnet-ef.exe");
            Console.WriteLine($"Set DotNetEfFullPath: {dotnetEFFullPath}");
            testProcessInfo.Environment["DotNetEfFullPath"] = dotnetEFFullPath;

            Directory.CreateDirectory(nugetRestore);

            // Rename default.runner.json to xunit.runner.json if there is not a custom one from the project
            if (!File.Exists("xunit.runner.json"))
            {
                File.Copy("default.runner.json", "xunit.runner.json");
            }

            Console.WriteLine("Displaying directory contents");
            foreach (var file in Directory.EnumerateFiles("./"))
            {
                Console.WriteLine(Path.GetFileName(file));
            }

            var testProcess = Process.Start(testProcessInfo);
            testProcess.BeginOutputReadLine();

            var discoveredTestsBuilder = new StringBuilder();
            testProcess.OutputDataReceived += (_, d) =>
            {
                if (d.Data != null)
                {
                    discoveredTestsBuilder.AppendLine(d.Data);
                }
            };
            testProcess.WaitForExit();

            var discoveredTests = discoveredTestsBuilder.ToString();
            if (discoveredTests.Contains("Exception thrown"))
            {
                Console.WriteLine("Exception thrown during test discovery.");
                Console.WriteLine(discoveredTests);
                Environment.Exit(1);
                return;
            }

            var exitCode = 0;
            if (string.Equals(quarantined, "true") || string.Equals(quarantined, "1"))
            {
                Console.WriteLine("Running quarantined tests.");

                // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                testProcessInfo.Arguments = $"vstest {target} --logger:xunit --TestCaseFilter:\"Quarantined=true\"";

                testProcess = Process.Start(testProcessInfo);
                testProcess.BeginOutputReadLine();

                testProcess.OutputDataReceived += (_, d) =>
                {
                    if (d.Data != null)
                    {
                        Console.WriteLine(d.Data);
                    }
                };
                testProcess.WaitForExit();

                if (testProcess.ExitCode != 0)
                {
                    Console.WriteLine($"Failure in quarantined tests. Exit code: {testProcess.ExitCode}.");
                }
            }
            else
            {
                Console.WriteLine("Running non-quarantined tests.");

                // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                testProcessInfo.Arguments = $"vstest {target} --logger:xunit --TestCaseFilter:\"Quarantined!=true\"";

                testProcess = Process.Start(testProcessInfo);
                testProcess.BeginOutputReadLine();

                testProcess.OutputDataReceived += (_, d) =>
                {
                    if (d.Data != null)
                    {
                        Console.WriteLine(d.Data);
                    }
                };
                testProcess.WaitForExit();

                if (testProcess.ExitCode != 0)
                {
                    Console.WriteLine($"Failure in non-quarantined tests. Exit code: {testProcess.ExitCode}.");
                    exitCode = testProcess.ExitCode;
                }
            }

            Console.WriteLine("Copying TestResults/TestResults.xml to .");
            File.Copy("TestResults/TestResults.xml", "testResults.xml");

            var HELIX_WORKITEM_UPLOAD_ROOT = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            Console.WriteLine($"Copying artifacts/logs to {HELIX_WORKITEM_UPLOAD_ROOT}/");
            foreach (var file in Directory.EnumerateFiles("artifacts/log", "*.log", SearchOption.AllDirectories))
            {
                Console.WriteLine($"Copying: {file}");
                File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, Path.GetFileName(file)));
                File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, "..", Path.GetFileName(file)));
            }

            Environment.Exit(exitCode);
        }
    }
}
