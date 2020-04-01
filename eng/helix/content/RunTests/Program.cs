// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RunTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var command = new RootCommand()
            {
                new Option(
                    aliases: new string[] { "--target", "-t" },
                    description: "The test dll to run")
                    { Argument = new Argument<string>(), Required = true },

                new Option(
                    aliases: new string[] { "--sdk" },
                    description: "The version of the sdk being used")
                { Argument = new Argument<string>(), Required = true },

                new Option(
                    aliases: new string[] { "--runtime" },
                    description: "The version of the runtime being used")
                { Argument = new Argument<string>(), Required = true },

                new Option(
                    aliases: new string[] { "--queue" },
                    description: "The name of the Helix queue being run on")
                { Argument = new Argument<string>(), Required = true },

                new Option(
                    aliases: new string[] { "--arch" },
                    description: "The architecture being run on")
                { Argument = new Argument<string>(), Required = true },

                new Option(
                    aliases: new string[] { "--quarantined" },
                    description: "Whether quarantined tests should run or not")
                { Argument = new Argument<bool>(), Required = true },

                new Option(
                    aliases: new string[] { "--ef" },
                    description: "The version of the EF tool to use")
                { Argument = new Argument<string>(), Required = true },
            };

            var parseResult = command.Parse(args);
            var target = parseResult.ValueForOption<string>("--target");
            var sdkVersion = parseResult.ValueForOption<string>("--sdk");
            var runtimeVersion = parseResult.ValueForOption<string>("--runtime");
            var helixQueue = parseResult.ValueForOption<string>("--queue");
            var architecture = parseResult.ValueForOption<string>("--arch");
            var quarantined = parseResult.ValueForOption<bool>("--quarantined");
            var efVersion = parseResult.ValueForOption<string>("--ef");

            var HELIX_WORKITEM_ROOT = Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT");

            var path = Environment.GetEnvironmentVariable("PATH");
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            // Rename default.NuGet.config to NuGet.config if there is not a custom one from the project
            // We use a local NuGet.config file to avoid polluting global machine state and avoid relying on global machine state
            if (!File.Exists("NuGet.config"))
            {
                File.Copy("default.NuGet.config", "NuGet.config");
            }

            var environmentVariables = new Dictionary<string, string>();
            environmentVariables.Add("PATH", path);
            environmentVariables.Add("DOTNET_ROOT", dotnetRoot);
            environmentVariables.Add("helix", helixQueue);

            Console.WriteLine($"Current Directory: {HELIX_WORKITEM_ROOT}");
            var helixDir = HELIX_WORKITEM_ROOT;
            Console.WriteLine($"Setting HELIX_DIR: {helixDir}");
            environmentVariables.Add("HELIX_DIR", helixDir);
            environmentVariables.Add("NUGET_FALLBACK_PACKAGES", helixDir);
            var nugetRestore = Path.Combine(helixDir, "nugetRestore");
            Console.WriteLine($"Creating nuget restore directory: {nugetRestore}");
            environmentVariables.Add("NUGET_RESTORE", nugetRestore);
            var dotnetEFFullPath = Path.Combine(nugetRestore, $"dotnet-ef/{efVersion}/tools/netcoreapp3.1/any/dotnet-ef.exe");
            Console.WriteLine($"Set DotNetEfFullPath: {dotnetEFFullPath}");
            environmentVariables.Add("DotNetEfFullPath", dotnetEFFullPath);

            Console.WriteLine("Checking for Microsoft.AspNetCore.App/");
            if (Directory.Exists("Microsoft.AspNetCore.App"))
            {
                Console.WriteLine($"Found Microsoft.AspNetCore.App/, copying to {dotnetRoot}/shared/Microsoft.AspNetCore.App/{runtimeVersion}");
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

                // Write nuget sources to console, useful for debugging purposes
                await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    "nuget list source",
                    environmentVariables: environmentVariables,
                    outputDataReceived: Console.WriteLine,
                    errorDataReceived: Console.WriteLine);

                await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    $"tool install dotnet-ef --global --version {efVersion}",
                    environmentVariables: environmentVariables);

                // ';' is the path separator on Windows, and ':' on Unix
                path += RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
                path += $"{Environment.GetEnvironmentVariable("DOTNET_CLI_HOME")}/.dotnet/tools";
                environmentVariables["PATH"] = path;
            }

            Directory.CreateDirectory(nugetRestore);

            // Rename default.runner.json to xunit.runner.json if there is not a custom one from the project
            if (!File.Exists("xunit.runner.json"))
            {
                File.Copy("default.runner.json", "xunit.runner.json");
            }

            Console.WriteLine();
            Console.WriteLine("Displaying directory contents:");
            foreach (var file in Directory.EnumerateFiles("./"))
            {
                Console.WriteLine(Path.GetFileName(file));
            }
            foreach (var file in Directory.EnumerateDirectories("./"))
            {
                Console.WriteLine(Path.GetFileName(file));
            }
            Console.WriteLine();

            // Run test discovery so we know if there are tests to run
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
            if (quarantined)
            {
                Console.WriteLine("Running quarantined tests.");

                // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                var result = await ProcessUtil.RunAsync($"{dotnetRoot}/dotnet",
                    commonTestArgs + " --TestCaseFilter:\"Quarantined=true\"",
                    environmentVariables: environmentVariables,
                    outputDataReceived: Console.WriteLine,
                    errorDataReceived: Console.WriteLine,
                    throwOnError: false);

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
                    errorDataReceived: Console.Error.WriteLine,
                    throwOnError: false);

                if (result.ExitCode != 0)
                {
                    Console.WriteLine($"Failure in non-quarantined tests. Exit code: {result.ExitCode}.");
                    exitCode = result.ExitCode;
                }
            }

            // 'testResults.xml' is the file Helix looks for when processing test results
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
                    var logName = $"{Path.GetFileName(Path.GetDirectoryName(file))}_{Path.GetFileName(file)}";
                    Console.WriteLine($"Copying: {file} to {Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, logName)}");
                    // Need to copy to HELIX_WORKITEM_UPLOAD_ROOT and HELIX_WORKITEM_UPLOAD_ROOT/../ in order for Azure Devops attachments to link properly and for Helix to store the logs
                    File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, logName));
                    File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, "..", logName));
                }
            }
            else
            {
                Console.WriteLine("No logs found in artifacts/log");
            }

            Console.WriteLine("Completed Helix job.");
            Environment.Exit(exitCode);
        }
    }
}
