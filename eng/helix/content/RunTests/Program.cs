// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RunTests
{
    class Program
    {
        static async Task Main(string[] args)
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

            // Rename default.NuGet.config to NuGet.config if there is not a custom one from the project
            if (!File.Exists("NuGet.config"))
            {
                File.Copy("default.NuGet.config", "NuGet.config");
            }

            var environmentVariables = new Dictionary<string, string>();
            environmentVariables.Add("PATH", path);
            environmentVariables.Add("DOTNET_ROOT", dotnetRoot);
            environmentVariables.Add("HELIX", helixQueue);

            Console.WriteLine($"Current Directory: {HELIX_WORKITEM_ROOT}");
            var helixDir = Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT");
            Console.WriteLine($"Setting HELIX_DIR: {helixDir}");
            environmentVariables.Add("HELIX_DIR", helixDir);
            environmentVariables.Add("NUGET_FALLBACK_PACKAGES", helixDir);
            var nugetRestore = Path.Combine(helixDir, "nugetRestore");
            Console.WriteLine($"Creating nuget restore directory: {nugetRestore}");
            environmentVariables.Add("NUGET_RESTORE", nugetRestore);
            var dotnetEFFullPath = Path.Combine(nugetRestore, $"dotnet-ef/{efVersion}/tools/netcoreapp3.1/any/dotnet-ef.exe");
            Console.WriteLine($"Set DotNetEfFullPath: {dotnetEFFullPath}");
            environmentVariables.Add("DotNetEfFullPath", dotnetEFFullPath);

            Console.WriteLine("Checking for Microsoft.AspNetCore.App");
            if (Directory.Exists("Microsoft.AspNetCore.App"))
            {
                Console.WriteLine($"Found Microsoft.AspNetCore.App, copying to {dotnetRoot}/shared/Microsoft.AspNetCore.App/{runtimeVersion}");
                foreach (var file in Directory.EnumerateFiles("Microsoft.AspNetCore.App", "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(file, $"{dotnetRoot}/shared/Microsoft.AspNetCore.App/{runtimeVersion}", overwrite: true);
                }

                Console.WriteLine($"Adding current directory to nuget sources: {HELIX_WORKITEM_ROOT}");

                await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    $"nuget add source {HELIX_WORKITEM_ROOT} --configfile NuGet.config",
                    environmentVariables: environmentVariables);

                await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    "nuget add source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json --configfile NuGet.config",
                    environmentVariables: environmentVariables);

                await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    "nuget list source",
                    environmentVariables: environmentVariables);

                await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    $"tool install dotnet-ef --global --version {efVersion}",
                    environmentVariables: environmentVariables);

                path += $";{Environment.GetEnvironmentVariable("DOTNET_CLI_HOME")}/.dotnet/tools";
                environmentVariables["PATH"] = path;
            }

            Directory.CreateDirectory(nugetRestore);

            // Rename default.runner.json to xunit.runner.json if there is not a custom one from the project
            if (!File.Exists("xunit.runner.json"))
            {
                File.Copy("default.runner.json", "xunit.runner.json");
            }

            Console.WriteLine();
            Console.WriteLine("Displaying directory contents");
            foreach (var file in Directory.EnumerateFiles("./"))
            {
                Console.WriteLine(Path.GetFileName(file));
            }
            Console.WriteLine();

            var discoveryResult = await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                $"vstest {target} -lt",
                environmentVariables: environmentVariables);

            if (discoveryResult.StandardOutput.Contains("Exception thrown"))
            {
                Console.WriteLine("Exception thrown during test discovery.");
                Console.WriteLine(discoveryResult.StandardOutput);
                Environment.Exit(1);
                return;
            }

            var exitCode = 0;
            var commonTestArgs = $"vstest {target} --logger:xunit --logger:\"console;verbosity=normal\" --blame";
            if (string.Equals(quarantined, "true") || string.Equals(quarantined, "1"))
            {
                Console.WriteLine("Running quarantined tests.");

                // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md

                var result = await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    commonTestArgs + " --TestCaseFilter:\"Quarantined=true\"",
                    environmentVariables: environmentVariables,
                    outputDataReceived: Console.WriteLine,
                    errorDataReceived: Console.WriteLine);

                if (result.ExitCode != 0)
                {
                    Console.WriteLine($"Failure in quarantined tests. Exit code: {result.ExitCode}.");
                }
            }
            else
            {
                Console.WriteLine("Running non-quarantined tests.");

                // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                var result = await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    commonTestArgs + " --TestCaseFilter:\"Quarantined!=true\"",
                    environmentVariables: environmentVariables,
                    outputDataReceived: Console.WriteLine,
                    errorDataReceived: Console.WriteLine);

                if (result.ExitCode != 0)
                {
                    Console.WriteLine($"Failure in non-quarantined tests. Exit code: {result.ExitCode}.");
                    exitCode = result.ExitCode;
                }
            }

            Console.WriteLine();
            if (File.Exists("TestResults/TestResults.xml"))
            {
                Console.WriteLine("Copying TestResults/TestResults.xml to ./testResults.xml");
                File.Copy("TestResults/TestResults.xml", "testResults.xml");
            }
            else
            {
                Console.WriteLine("No test results found.");
            }

            var HELIX_WORKITEM_UPLOAD_ROOT = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            Console.WriteLine($"Copying artifacts/log/ to {HELIX_WORKITEM_UPLOAD_ROOT}/");
            if (Directory.Exists("artifacts/log"))
            {
                foreach (var file in Directory.EnumerateFiles("artifacts/log", "*.log", SearchOption.AllDirectories))
                {
                    // Combine the directory name + log name for the copied log file name to avoid overwriting duplicate test names in different test projects
                    var logName = $"{Path.GetDirectoryName(file)}_{Path.GetFileName(file)}";
                    Console.WriteLine($"Copying: {file} to {Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, logName)}");
                    File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, logName));
                    File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, "..", logName));
                }
            }
            else
            {
                Console.WriteLine("No logs found in artifacts/log");
            }

            Environment.Exit(exitCode);
        }
    }
}
