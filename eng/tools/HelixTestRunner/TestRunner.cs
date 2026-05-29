// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HelixTestRunner;

public class TestRunner
{
    public TestRunner(HelixTestRunnerOptions options)
    {
        Options = options;
        EnvironmentVariables = new Dictionary<string, string>();
    }

    public HelixTestRunnerOptions Options { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; }

    public bool SetupEnvironment()
    {
        try
        {
            EnvironmentVariables.Add("DOTNET_CLI_HOME", Options.HELIX_WORKITEM_ROOT);
            EnvironmentVariables.Add("PATH", Options.Path);
            EnvironmentVariables.Add("helix", Options.HelixQueue);

            ProcessUtil.PrintMessage($"Current Directory: {Options.HELIX_WORKITEM_ROOT}");
            var helixDir = Options.HELIX_WORKITEM_ROOT;
            ProcessUtil.PrintMessage($"Setting HELIX_DIR: {helixDir}");
            EnvironmentVariables.Add("HELIX_DIR", helixDir);
            EnvironmentVariables.Add("NUGET_FALLBACK_PACKAGES", helixDir);
            var nugetRestore = Path.Combine(helixDir, "nugetRestore");
            EnvironmentVariables.Add("NUGET_RESTORE", nugetRestore);
            var dotnetEFFullPath = Path.Combine(nugetRestore, helixDir, "dotnet-ef.exe");
            ProcessUtil.PrintMessage($"Set DotNetEfFullPath: {dotnetEFFullPath}");
            EnvironmentVariables.Add("DotNetEfFullPath", dotnetEFFullPath);
            var dumpPath = Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");
            ProcessUtil.PrintMessage($"Set VSTEST_DUMP_PATH: {dumpPath}");
            EnvironmentVariables.Add("VSTEST_DUMP_PATH", dumpPath);
            EnvironmentVariables.Add("DOTNET_CLI_VSTEST_TRACE", "1");

            if (Options.InstallPlaywright)
            {
                // Playwright will download and look for browsers to this directory
                var playwrightBrowsers = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
                ProcessUtil.PrintMessage($"Setting PLAYWRIGHT_BROWSERS_PATH: {playwrightBrowsers}");
                EnvironmentVariables.Add("PLAYWRIGHT_BROWSERS_PATH", playwrightBrowsers);
            }
            else
            {
                ProcessUtil.PrintMessage($"Skipping setting PLAYWRIGHT_BROWSERS_PATH");
            }

            ProcessUtil.PrintMessage($"Creating nuget restore directory: {nugetRestore}");
            Directory.CreateDirectory(nugetRestore);

            // Rename default.runner.json to xunit.runner.json if there is not a custom one from the project
            if (!File.Exists("xunit.runner.json"))
            {
                File.Copy("default.runner.json", "xunit.runner.json");
            }

            DisplayContents(Path.Combine(Options.DotnetRoot, "host", "fxr"));
            DisplayContents(Path.Combine(Options.DotnetRoot, "shared", "Microsoft.NETCore.App"));
            DisplayContents(Path.Combine(Options.DotnetRoot, "shared", "Microsoft.AspNetCore.App"));
            DisplayContents(Path.Combine(Options.DotnetRoot, "packs", "Microsoft.AspNetCore.App.Ref"));

            return true;
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception in SetupEnvironment: {e}");
            return false;
        }
    }

    public void DisplayContents(string path = "./")
    {
        try
        {
            Console.WriteLine();
            ProcessUtil.PrintMessage($"Displaying directory contents for {path}:");
            foreach (var file in Directory.EnumerateFiles(path))
            {
                ProcessUtil.PrintMessage(Path.GetFileName(file));
            }
            foreach (var file in Directory.EnumerateDirectories(path))
            {
                ProcessUtil.PrintMessage(Path.GetFileName(file));
            }
            Console.WriteLine();
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception in DisplayContents: {e}");
        }
    }

    public bool InstallPlaywright()
    {
        try
        {
            ProcessUtil.PrintMessage($"Installing Playwright Browsers to {Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH")}");

            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });

            DisplayContents(Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH"));
            return true;
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception installing playwright: {e}");
            return false;
        }
    }

    public async Task<bool> InstallDotnetToolsAsync()
    {
        const string filename = "NuGet.config";
        const string backupFilename = "NuGet.save";
        var correlationPayload = Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD");

        try
        {
            // Extract tools directly from nupkgs in the correlation payload instead of running
            // `dotnet tool install`, which invokes the dotnet CLI and can hang on some machines.
            // The nupkgs are zip files containing the tool binaries under tools/<tfm>/any/.
            ExtractToolFromNupkg(correlationPayload, "dotnet-dump", "dotnet-dump");
            ExtractToolFromNupkg(correlationPayload, "dotnet-ef", "dotnet-ef");
            ExtractToolFromNupkg(correlationPayload, "dotnet-serve", "dotnet-serve");
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception extracting tools from nupkgs: {e}");
            ProcessUtil.PrintMessage("Falling back to dotnet tool install...");

            // Fall back to the original approach if extraction fails
            try
            {
                // Do not use network for dotnet tool installations.
                File.Move(filename, backupFilename);

                await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                    $"tool install dotnet-dump --tool-path {Options.HELIX_WORKITEM_ROOT} --add-source {correlationPayload}",
                    environmentVariables: EnvironmentVariables,
                    outputDataReceived: ProcessUtil.PrintMessage,
                    errorDataReceived: ProcessUtil.PrintErrorMessage,
                    throwOnError: false,
                    cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

                var efVersionArg = "";
                var efPackages = Directory.GetFiles(correlationPayload, "dotnet-ef.*.nupkg");
                if (efPackages.Length > 0)
                {
                    var efFileName = Path.GetFileNameWithoutExtension(efPackages[0]);
                    var version = efFileName["dotnet-ef.".Length..];
                    efVersionArg = $"--version {version} ";
                }

                await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                    $"tool install dotnet-ef {efVersionArg}--tool-path {Options.HELIX_WORKITEM_ROOT} --add-source {correlationPayload}",
                    environmentVariables: EnvironmentVariables,
                    outputDataReceived: ProcessUtil.PrintMessage,
                    errorDataReceived: ProcessUtil.PrintErrorMessage,
                    throwOnError: false,
                    cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

                await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                    $"tool install dotnet-serve --tool-path {Options.HELIX_WORKITEM_ROOT} --add-source {correlationPayload}",
                    environmentVariables: EnvironmentVariables,
                    outputDataReceived: ProcessUtil.PrintMessage,
                    errorDataReceived: ProcessUtil.PrintErrorMessage,
                    throwOnError: false,
                    cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);
            }
            catch (Exception fallbackEx)
            {
                ProcessUtil.PrintMessage($"Exception in fallback InstallDotnetTools: {fallbackEx}");
                return false;
            }
            finally
            {
                if (File.Exists(backupFilename))
                {
                    File.Move(backupFilename, filename);
                }
            }
        }

        try
        {
            // Add the work item root as a NuGet source by editing the config file directly
            // instead of invoking `dotnet nuget add source`.
            AddNuGetSource(Options.HELIX_WORKITEM_ROOT);
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception adding NuGet source: {e}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Extracts a dotnet tool from its nupkg (zip) in the correlation payload and creates
    /// a shell/batch shim so it can be invoked by name from PATH.
    /// </summary>
    private void ExtractToolFromNupkg(string correlationPayload, string toolNupkgPrefix, string toolName)
    {
        var nupkgs = Directory.GetFiles(correlationPayload, $"{toolNupkgPrefix}.*.nupkg");
        if (nupkgs.Length == 0)
        {
            ProcessUtil.PrintMessage($"Warning: No {toolNupkgPrefix} nupkg found in {correlationPayload}");
            return;
        }

        var nupkgPath = nupkgs[0];
        var extractDir = Path.Combine(Options.HELIX_WORKITEM_ROOT, ".tools", toolName);
        ProcessUtil.PrintMessage($"Extracting {toolName} from {Path.GetFileName(nupkgPath)} to {extractDir}");

        System.IO.Compression.ZipFile.ExtractToDirectory(nupkgPath, extractDir, overwriteFiles: true);

        // Find the tool DLL inside the extracted nupkg under tools/<tfm>/any/
        var toolsDir = Path.Combine(extractDir, "tools");
        if (!Directory.Exists(toolsDir))
        {
            ProcessUtil.PrintMessage($"Warning: No tools/ directory in {toolNupkgPrefix} nupkg");
            return;
        }

        // Pick the highest TFM directory available, sorting by parsed version number
        // to handle multi-digit versions correctly (e.g., net10.0 > net9.0).
        string toolDll = null;
        var tfmDirs = Directory.GetDirectories(toolsDir)
            .Select(d => (dir: d, name: Path.GetFileName(d)))
            .OrderByDescending(d =>
            {
                // Parse version from TFM folder name like "net8.0", "net10.0", "netcoreapp3.1"
                var numStr = new string(d.name.SkipWhile(c => !char.IsDigit(c)).ToArray());
                return Version.TryParse(numStr, out var v) ? v : new Version(0, 0);
            });
        foreach (var (tfmDir, _) in tfmDirs)
        {
            var anyDir = Path.Combine(tfmDir, "any");
            if (Directory.Exists(anyDir))
            {
                var candidate = Path.Combine(anyDir, $"{toolName}.dll");
                if (File.Exists(candidate))
                {
                    toolDll = candidate;
                    break;
                }
            }
        }

        if (toolDll is null)
        {
            ProcessUtil.PrintMessage($"Warning: Could not find {toolName}.dll in extracted nupkg");
            return;
        }

        // Create a shim script in the work item root (which is on PATH)
        var dotnetExe = Path.Combine(Options.DotnetRoot, "dotnet");
        if (OperatingSystem.IsWindows())
        {
            // Create a .cmd shim only — a .cmd file on PATH is sufficient for tool invocation
            // on Windows. Do not create a fake .exe since it would be an invalid executable.
            var cmdShimPath = Path.Combine(Options.HELIX_WORKITEM_ROOT, $"{toolName}.cmd");
            File.WriteAllText(cmdShimPath, $"@\"{dotnetExe}\" exec \"{toolDll}\" %*\r\n");
        }
        else
        {
            var shimPath = Path.Combine(Options.HELIX_WORKITEM_ROOT, toolName);
            File.WriteAllText(shimPath, $"#!/bin/sh\nexec \"{dotnetExe}\" exec \"{toolDll}\" \"$@\"\n");
            // Make executable
            File.SetUnixFileMode(shimPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        ProcessUtil.PrintMessage($"Installed {toolName} shim -> {toolDll}");
    }

    /// <summary>
    /// Adds a NuGet source to the NuGet.config file directly by editing the XML,
    /// avoiding the need to invoke `dotnet nuget add source`.
    /// </summary>
    private static void AddNuGetSource(string sourcePath)
    {
        const string configFile = "NuGet.config";
        if (!File.Exists(configFile))
        {
            ProcessUtil.PrintMessage($"Warning: {configFile} not found, skipping NuGet source addition");
            return;
        }

        ProcessUtil.PrintMessage($"Adding NuGet source: {sourcePath}");

        var doc = new System.Xml.XmlDocument();
        doc.Load(configFile);

        var packageSources = doc.SelectSingleNode("//packageSources");
        if (packageSources is null)
        {
            ProcessUtil.PrintMessage("Warning: No <packageSources> element found in NuGet.config");
            return;
        }

        // Update existing entry if present, otherwise add a new one
        var existing = doc.SelectSingleNode("//packageSources/add[@key='HelixWorkItemRoot']") as System.Xml.XmlElement;
        if (existing is not null)
        {
            existing.SetAttribute("value", sourcePath);
        }
        else
        {
            var addElement = doc.CreateElement("add");
            addElement.SetAttribute("key", "HelixWorkItemRoot");
            addElement.SetAttribute("value", sourcePath);
            packageSources.AppendChild(addElement);
        }
        doc.Save(configFile);

        ProcessUtil.PrintMessage($"Added NuGet source 'HelixWorkItemRoot' = {sourcePath}");
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
                ProcessUtil.PrintMessage("Exception thrown during test discovery.");
                ProcessUtil.PrintMessage(discoveryResult.StandardOutput);
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception in CheckTestDiscovery: {e}");
            return false;
        }
    }

    public async Task<int> RunTestsAsync()
    {
        var exitCode = 0;
        try
        {
            // Timeout test run 5 minutes before the Helix job would timeout
            var testProcessTimeout = Options.Timeout.Subtract(TimeSpan.FromMinutes(5));
            var cts = new CancellationTokenSource(testProcessTimeout);
            var diagLog = Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT"), "vstest.log");
            var commonTestArgs = $"test {Options.Target} --diag:{diagLog} --logger xunit --logger \"console;verbosity=normal\" " +
                                 "--blame-crash --blame-hang-timeout 15m";
            if (Options.Quarantined)
            {
                ProcessUtil.PrintMessage("Running quarantined tests.");

                // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                var result = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                    commonTestArgs + " --filter \"Quarantined=true\"",
                    environmentVariables: EnvironmentVariables,
                    outputDataReceived: ProcessUtil.PrintMessage,
                    errorDataReceived: ProcessUtil.PrintErrorMessage,
                    throwOnError: false,
                    cancellationToken: cts.Token);

                if (cts.Token.IsCancellationRequested)
                {
                    ProcessUtil.PrintMessage($"Quarantined tests exceeded configured timeout: {testProcessTimeout.TotalMinutes}m.");
                }
                if (result.ExitCode != 0)
                {
                    ProcessUtil.PrintMessage($"Failure in quarantined tests. Exit code: {result.ExitCode}.");
                }
            }
            else
            {
                ProcessUtil.PrintMessage("Running non-quarantined tests.");

                // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                var result = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                    commonTestArgs + " --filter \"Quarantined!=true|Quarantined=false\"",
                    environmentVariables: EnvironmentVariables,
                    outputDataReceived: ProcessUtil.PrintMessage,
                    errorDataReceived: ProcessUtil.PrintErrorMessage,
                    throwOnError: false,
                    cancellationToken: cts.Token);

                if (cts.Token.IsCancellationRequested)
                {
                    ProcessUtil.PrintMessage($"Non-quarantined tests exceeded configured timeout: {testProcessTimeout.TotalMinutes}m.");
                }
                if (result.ExitCode != 0)
                {
                    ProcessUtil.PrintMessage($"Failure in non-quarantined tests. Exit code: {result.ExitCode}.");
                    exitCode = result.ExitCode;
                }
            }
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception in HelixTestRunner: {e}");
            exitCode = 1;
        }
        return exitCode;
    }

    public void UploadResults()
    {
        // 'testResults.xml' is the file Helix looks for when processing test results
        ProcessUtil.PrintMessage("Trying to upload results...");
        if (File.Exists("TestResults/TestResults.xml"))
        {
            ProcessUtil.PrintMessage("Copying TestResults/TestResults.xml to ./testResults.xml");
            File.Copy("TestResults/TestResults.xml", "testResults.xml", overwrite: true);
        }
        else
        {
            ProcessUtil.PrintMessage("No test results found.");
        }

        var HELIX_WORKITEM_UPLOAD_ROOT = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
        if (string.IsNullOrEmpty(HELIX_WORKITEM_UPLOAD_ROOT))
        {
            ProcessUtil.PrintMessage("No HELIX_WORKITEM_UPLOAD_ROOT specified, skipping log copy");
            return;
        }
        ProcessUtil.PrintMessage($"Copying artifacts/log/ to {HELIX_WORKITEM_UPLOAD_ROOT}/");
        if (Directory.Exists("artifacts/log"))
        {
            foreach (var file in Directory.EnumerateFiles("artifacts/log", "*.log", SearchOption.AllDirectories))
            {
                // Combine the directory name + log name for the copied log file name to avoid overwriting
                // duplicate test names in different test projects
                var logName = $"{Path.GetFileName(Path.GetDirectoryName(file))}_{Path.GetFileName(file)}";
                ProcessUtil.PrintMessage($"Copying: {file} to {Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, logName)}");
                File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, logName));
            }
        }
        else
        {
            ProcessUtil.PrintMessage("No logs found in artifacts/log");
        }
        ProcessUtil.PrintMessage($"Copying TestResults/**/Sequence*.xml to {HELIX_WORKITEM_UPLOAD_ROOT}/");
        if (Directory.Exists("TestResults"))
        {
            foreach (var file in Directory.EnumerateFiles("TestResults", "Sequence*.xml", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                ProcessUtil.PrintMessage($"Copying: {file} to {Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, fileName)}");
                File.Copy(file, Path.Combine(HELIX_WORKITEM_UPLOAD_ROOT, fileName));
            }
        }
        else
        {
            ProcessUtil.PrintMessage("No TestResults directory found.");
        }
    }
}
