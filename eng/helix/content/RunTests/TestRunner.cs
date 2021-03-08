// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
#if INSTALLPLAYWRIGHT
using PlaywrightSharp;
#endif

namespace RunTests
{
    public class TestRunner
    {
        public TestRunner(RunTestsOptions options)
        {
            Options = options;
            EnvironmentVariables = new Dictionary<string, string>();
        }

        public RunTestsOptions Options { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        public bool SetupEnvironment()
        {
            try
            {
                EnvironmentVariables.Add("PATH", Options.Path);
                EnvironmentVariables.Add("helix", Options.HelixQueue);

                Console.WriteLine($"Current Directory: {Options.HELIX_WORKITEM_ROOT}");
                var helixDir = Options.HELIX_WORKITEM_ROOT;
                Console.WriteLine($"Setting HELIX_DIR: {helixDir}");
                EnvironmentVariables.Add("HELIX_DIR", helixDir);
                EnvironmentVariables.Add("NUGET_FALLBACK_PACKAGES", helixDir);
                var nugetRestore = Path.Combine(helixDir, "nugetRestore");
                EnvironmentVariables.Add("NUGET_RESTORE", nugetRestore);
                var dotnetEFFullPath = Path.Combine(nugetRestore, helixDir, "dotnet-ef.exe");
                Console.WriteLine($"Set DotNetEfFullPath: {dotnetEFFullPath}");
                EnvironmentVariables.Add("DotNetEfFullPath", dotnetEFFullPath);

#if INSTALLPLAYWRIGHT
                // Playwright will download and look for browsers to this directory
                var playwrightBrowsers = Path.Combine(helixDir, "ms-playwright");
                Console.WriteLine($"Setting PLAYWRIGHT_BROWSERS_PATH: {playwrightBrowsers}");
                EnvironmentVariables.Add("PLAYWRIGHT_BROWSERS_PATH", playwrightBrowsers);
#endif
    
                Console.WriteLine($"Creating nuget restore directory: {nugetRestore}");
                Directory.CreateDirectory(nugetRestore);

                // Rename default.runner.json to xunit.runner.json if there is not a custom one from the project
                if (!File.Exists("xunit.runner.json"))
                {
                    File.Copy("default.runner.json", "xunit.runner.json");
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in SetupEnvironment: {e.ToString()}");
                return false;
            }
        }

        public void DisplayContents(string path = "./")
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine($"Displaying directory contents for {path}:");
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    Console.WriteLine(Path.GetFileName(file));
                }
                foreach (var file in Directory.EnumerateDirectories(path))
                {
                    Console.WriteLine(Path.GetFileName(file));
                }
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in DisplayContents: {e.ToString()}");
            }
        }

#if INSTALLPLAYWRIGHT
        public async Task<bool> InstallPlaywrightAsync()
        {
            try
            {
                Console.WriteLine($"Installing Playwright to {EnvironmentVariables["PLAYWRIGHT_BROWSERS_PATH"]}");
                await Playwright.InstallAsync(EnvironmentVariables["PLAYWRIGHT_BROWSERS_PATH"]);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception installing playwright: {e.ToString()}");
                return false;
            }
        }
#endif
        
        public async Task<bool> InstallAspNetAppIfNeededAsync()
        {
            try
            {
                if (File.Exists(Options.AspNetRuntime))
                {
                    var appRuntimePath = $"{Options.DotnetRoot}/shared/Microsoft.AspNetCore.App/{Options.RuntimeVersion}";
                    Console.WriteLine($"Creating directory: {appRuntimePath}");
                    Directory.CreateDirectory(appRuntimePath);
                    Console.WriteLine($"Set ASPNET_RUNTIME_PATH: {appRuntimePath}");
                    EnvironmentVariables.Add("ASPNET_RUNTIME_PATH", appRuntimePath);
                    Console.WriteLine($"Found AspNetRuntime: {Options.AspNetRuntime}, extracting *.txt,json,dll,xml to {appRuntimePath}");
                    using (var archive = ZipFile.OpenRead(Options.AspNetRuntime))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            // These are the only extensions that end up in the shared fx directory
                            if (entry.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                                entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                                entry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            {
                                entry.ExtractToFile(Path.Combine(appRuntimePath, entry.Name), overwrite: true);
                            }
                        }
                    }

                    DisplayContents(appRuntimePath);

                    Console.WriteLine($"Adding current directory to nuget sources: {Options.HELIX_WORKITEM_ROOT}");

                    await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                        $"nuget add source {Options.HELIX_WORKITEM_ROOT} --configfile NuGet.config",
                        environmentVariables: EnvironmentVariables,
                        outputDataReceived: Console.WriteLine,
                        errorDataReceived: Console.Error.WriteLine,
                        throwOnError: false,
                        cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

                    // Write nuget sources to console, useful for debugging purposes
                    await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                        "nuget list source",
                        environmentVariables: EnvironmentVariables,
                        outputDataReceived: Console.WriteLine,
                        errorDataReceived: Console.Error.WriteLine,
                        throwOnError: false,
                        cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

                    await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                        $"tool install dotnet-ef --version {Options.EfVersion} --tool-path {Options.HELIX_WORKITEM_ROOT}",
                        environmentVariables: EnvironmentVariables,
                        outputDataReceived: Console.WriteLine,
                        errorDataReceived: Console.Error.WriteLine,
                        throwOnError: false,
                        cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

                    await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                        $"tool install dotnet-serve --tool-path {Options.HELIX_WORKITEM_ROOT}",
                        environmentVariables: EnvironmentVariables,
                        outputDataReceived: Console.WriteLine,
                        errorDataReceived: Console.Error.WriteLine,
                        throwOnError: false,
                        cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);
                    
                    // ';' is the path separator on Windows, and ':' on Unix
                    Options.Path += OperatingSystem.IsWindows() ? ";" : ":";
                    Options.Path += $"{Environment.GetEnvironmentVariable("DOTNET_CLI_HOME")}/.dotnet/tools";
                    EnvironmentVariables["PATH"] = Options.Path;
                }
                else
                {
                    Console.WriteLine($"No AspNetRuntime found: {Options.AspNetRuntime}, skipping...");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in InstallAspNetAppIfNeeded: {e.ToString()}");
                return false;
            }
        }

        public bool InstallAspNetRefIfNeeded()
        {
            try
            {
                if (File.Exists(Options.AspNetRef))
                {
                    var refPath = $"{Options.DotnetRoot}/packs/Microsoft.AspNetCore.App.Ref/{Options.RuntimeVersion}";
                    Console.WriteLine($"Found AspNetRef: {Options.AspNetRef}, extracting to {refPath}");
                    ZipFile.ExtractToDirectory(Options.AspNetRef, refPath);

                    DisplayContents(refPath);
                }
                else
                {
                    Console.WriteLine($"No AspNetRef found: {Options.AspNetRef}, skipping...");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in InstallAspNetRefIfNeeded: {e.ToString()}");
                return false;
            }
        }

        public async Task<bool> InstallDotnetDump()
        {
            try
            {
                await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                            $"tool install dotnet-dump --tool-path {Options.HELIX_WORKITEM_ROOT} --version 5.0.0-*",
                            environmentVariables: EnvironmentVariables,
                            outputDataReceived: Console.WriteLine,
                            errorDataReceived: Console.Error.WriteLine,
                            throwOnError: false);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in InstallDotnetDump: {e}");
                return false;
            }
        }

        public async Task<bool> CheckTestDiscoveryAsync()
        {
            try
            {
                // Run test discovery so we know if there are tests to run
                var discoveryResult = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                    $"vstest {Options.Target} -lt",
                    environmentVariables: EnvironmentVariables,
                    cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

                if (discoveryResult.StandardOutput.Contains("Exception thrown"))
                {
                    Console.WriteLine("Exception thrown during test discovery.");
                    Console.WriteLine(discoveryResult.StandardOutput);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in CheckTestDiscovery: {e.ToString()}");
                return false;
            }
        }

        public async Task<int> RunTestsAsync()
        {
            var exitCode = 0;
            try
            {
                // Timeout test run 5 minutes before the Helix job would timeout
                var cts = new CancellationTokenSource(Options.Timeout.Subtract(TimeSpan.FromMinutes(5)));
                var commonTestArgs = $"test {Options.Target} --logger:xunit --logger:\"console;verbosity=normal\" --blame \"CollectHangDump;TestTimeout=15m\"";
                if (Options.Quarantined)
                {
                    Console.WriteLine("Running quarantined tests.");

                    // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                    var result = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                        commonTestArgs + " --TestCaseFilter:\"Quarantined=true\"",
                        environmentVariables: EnvironmentVariables,
                        outputDataReceived: Console.WriteLine,
                        errorDataReceived: Console.Error.WriteLine,
                        throwOnError: false,
                        cancellationToken: cts.Token);

                    if (result.ExitCode != 0)
                    {
                        Console.WriteLine($"Failure in quarantined tests. Exit code: {result.ExitCode}.");
                    }
                }
                else
                {
                    Console.WriteLine("Running non-quarantined tests.");

                    // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                    var result = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                        commonTestArgs + " --TestCaseFilter:\"Quarantined!=true|Quarantined=false\"",
                        environmentVariables: EnvironmentVariables,
                        outputDataReceived: Console.WriteLine,
                        errorDataReceived: Console.Error.WriteLine,
                        throwOnError: false,
                        cancellationToken: cts.Token);

                    if (result.ExitCode != 0)
                    {
                        Console.WriteLine($"Failure in non-quarantined tests. Exit code: {result.ExitCode}.");
                        exitCode = result.ExitCode;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in RunTests: {e.ToString()}");
                exitCode = 1;
            }
            return exitCode;
        }

        public void UploadResults()
        {
            // 'testResults.xml' is the file Helix looks for when processing test results
            Console.WriteLine("Trying to upload results...");
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
            if (string.IsNullOrEmpty(HELIX_WORKITEM_UPLOAD_ROOT))
            {
                Console.WriteLine("No HELIX_WORKITEM_UPLOAD_ROOT specified, skipping log copy");
                return;
            }
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
            Console.WriteLine($"Copying TestResults/**/*.dmp to {HELIX_WORKITEM_UPLOAD_ROOT}/");
            if (Directory.Exists("TestResults"))
            {
                foreach (var file in Directory.EnumerateFiles("TestResults", "*.dmp", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(file);
                    Console.WriteLine($"Copying: {file} to {Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, fileName)}");
                    // Need to copy to HELIX_WORKITEM_UPLOAD_ROOT and HELIX_WORKITEM_UPLOAD_ROOT/../ in order for Azure Devops attachments to link properly and for Helix to store the logs
                    File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, fileName));
                    File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, "..", fileName));
                }
            }
            else
            {
                Console.WriteLine("No dmps found in TestResults");
            }
        }
    }
}
