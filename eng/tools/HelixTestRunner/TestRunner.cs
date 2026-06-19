// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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
            var dumpPath = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
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
                ProcessUtil.PrintMessage("Skipping setting PLAYWRIGHT_BROWSERS_PATH");
            }

            ProcessUtil.PrintMessage($"Creating nuget restore directory: {nugetRestore}");
            Directory.CreateDirectory(nugetRestore);

            // Set up xunit.runner.json for each target assembly directory.
            // For single-target (non-batched) items this runs once in the work item root.
            // For batched items, each assembly subdirectory needs its own copy.
            foreach (var workingDirectory in GetTargetWorkingDirectories())
            {
                EnsureTargetRunnerConfiguration(workingDirectory);
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
            // Do not use network for dotnet tool installations.
            File.Move(filename, backupFilename);

            // Install dotnet-dump first so we can catch any failures from running dotnet after this
            // (installing tools, running tests, etc.)
            await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                $"tool install dotnet-dump --tool-path {Options.HELIX_WORKITEM_ROOT} --add-source {correlationPayload}",
                environmentVariables: EnvironmentVariables,
                outputDataReceived: ProcessUtil.PrintMessage,
                errorDataReceived: ProcessUtil.PrintErrorMessage,
                throwOnError: false,
                cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

            // Install dotnet-ef with the exact version from the correlation payload to avoid
            // picking up a mismatched version from the global NuGet cache.
            var efVersionArg = "";
            var efPackages = Directory.GetFiles(correlationPayload, "dotnet-ef.*.nupkg");
            if (efPackages.Length > 0)
            {
                // Extract version from filename: dotnet-ef.{version}.nupkg
                var fileName = Path.GetFileNameWithoutExtension(efPackages[0]);
                var version = fileName["dotnet-ef.".Length..];
                efVersionArg = $"--version {version} ";
                ProcessUtil.PrintMessage($"Found dotnet-ef package in payload: {efPackages[0]}, version: {version}");
            }
            else
            {
                ProcessUtil.PrintMessage("Warning: No dotnet-ef nupkg found in correlation payload. Tool install may pick an incompatible version.");
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
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception in InstallDotnetTools: {e}");
            return false;
        }
        finally
        {
            File.Move(backupFilename, filename);
        }

        try
        {
            await AddHelixSourcesAsync();
        }
        catch (Exception e)
        {
            ProcessUtil.PrintMessage($"Exception in InstallDotnetTools: {e}");
            return false;
        }

        return true;
    }

    public async Task<bool> CheckTestDiscoveryAsync()
    {
        try
        {
            // Run test discovery to verify there are tests to run.
            // dotnet test accepts multiple DLL paths.
            var assemblyArgs = string.Join(" ", Options.Targets.Select(t => $"\"{t}\""));
            ProcessUtil.PrintMessage($"Running test discovery for {Options.Targets.Length} assembly(ies).");
            var discoveryResult = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                $"test {assemblyArgs} --list-tests",
                environmentVariables: EnvironmentVariables,
                cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2 * Options.Targets.Length)).Token);

            if (discoveryResult.StandardOutput.Contains("Exception thrown", StringComparison.Ordinal) ||
                discoveryResult.StandardError.Contains("Exception thrown", StringComparison.Ordinal))
            {
                ProcessUtil.PrintMessage("Exception thrown during test discovery.");
                ProcessUtil.PrintMessage(discoveryResult.StandardOutput);
                ProcessUtil.PrintMessage(discoveryResult.StandardError);
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
            // Timeout test run 5 minutes before the Helix job would timeout.
            // Batched items inherit the same timeout each individual work item was given
            // (the repo's HelixTimeout), set in helix.proj's BatchHelixWorkItems task.
            var testProcessTimeout = Options.Timeout.Subtract(TimeSpan.FromMinutes(5));
            if (testProcessTimeout <= TimeSpan.Zero)
            {
                testProcessTimeout = Options.Timeout;
            }

            var uploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT") ?? Directory.GetCurrentDirectory();
            var diagLog = Path.Combine(uploadRoot, "vstest.log");

            // Pass all target assemblies to a single dotnet test invocation.
            // dotnet test accepts multiple DLL paths and produces a unified test results file.
            var assemblyArgs = string.Join(" ", Options.Targets.Select(t => $"\"{t}\""));
            var commonTestArgs = $"test {assemblyArgs} --diag:{diagLog} --logger xunit --logger \"console;verbosity=normal\" " +
                                 "--blame-crash --blame-hang-timeout 15m";

            using var cts = new CancellationTokenSource(testProcessTimeout);

            var filter = Options.Quarantined
                ? "Quarantined=true"
                : "Quarantined!=true|Quarantined=false";
            var filterDesc = Options.Quarantined ? "quarantined" : "non-quarantined";

            ProcessUtil.PrintMessage($"Running {filterDesc} tests for {Options.Targets.Length} assembly(ies).");
            var result = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                commonTestArgs + $" --filter \"{filter}\"",
                environmentVariables: EnvironmentVariables,
                outputDataReceived: ProcessUtil.PrintMessage,
                errorDataReceived: ProcessUtil.PrintErrorMessage,
                throwOnError: false,
                cancellationToken: cts.Token);

            if (cts.Token.IsCancellationRequested)
            {
                ProcessUtil.PrintMessage($"Tests exceeded configured timeout: {testProcessTimeout.TotalMinutes}m.");
            }
            if (result.ExitCode != 0)
            {
                ProcessUtil.PrintMessage($"Failure in {filterDesc} tests. Exit code: {result.ExitCode}.");
                // Quarantined test failures are expected and should not fail the work item.
                if (!Options.Quarantined)
                {
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
        // 'testResults.xml' is the file Helix looks for when processing test results.
        // With a single dotnet test invocation (even with multiple DLLs), there's one
        // unified TestResults.xml.
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

        if (File.Exists("testResults.xml"))
        {
            CopyFileToUploadRoot("testResults.xml", HELIX_WORKITEM_UPLOAD_ROOT, "testResults.xml");
        }

        // Copy logs from each assembly's subdirectory
        foreach (var target in Options.Targets)
        {
            var workingDirectory = GetTargetWorkingDirectory(target);
            var assemblyName = GetSanitizedAssemblyName(target);
            var artifactsLogDirectory = Path.Combine(workingDirectory, "artifacts", "log");
            if (Directory.Exists(artifactsLogDirectory))
            {
                ProcessUtil.PrintMessage($"Copying artifacts/log/ to {HELIX_WORKITEM_UPLOAD_ROOT}/");
                foreach (var file in Directory.EnumerateFiles(artifactsLogDirectory, "*.log", SearchOption.AllDirectories))
                {
                    var logName = $"{assemblyName}_{Path.GetFileName(Path.GetDirectoryName(file))}_{Path.GetFileName(file)}";
                    CopyFileToUploadRoot(file, HELIX_WORKITEM_UPLOAD_ROOT, logName);
                }
            }
        }

        ProcessUtil.PrintMessage($"Copying TestResults/**/Sequence*.xml to {HELIX_WORKITEM_UPLOAD_ROOT}/");
        if (Directory.Exists("TestResults"))
        {
            foreach (var file in Directory.EnumerateFiles("TestResults", "Sequence*.xml", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                CopyFileToUploadRoot(file, HELIX_WORKITEM_UPLOAD_ROOT, fileName);
            }
        }
        else
        {
            ProcessUtil.PrintMessage("No TestResults directory found.");
        }
    }

    private async Task AddHelixSourcesAsync()
    {
        // Add the work item root as a NuGet source. Only the root NuGet.config matters —
        // dotnet test runs from the work item root.
        var nugetConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "NuGet.config");
        if (File.Exists(nugetConfigPath))
        {
            ProcessUtil.PrintMessage($"Adding current directory to nuget sources: {Options.HELIX_WORKITEM_ROOT}");
            await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                $"nuget add source \"{Options.HELIX_WORKITEM_ROOT}\" --configfile \"{nugetConfigPath}\"",
                environmentVariables: EnvironmentVariables,
                outputDataReceived: ProcessUtil.PrintMessage,
                errorDataReceived: ProcessUtil.PrintErrorMessage,
                throwOnError: false,
                cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

            await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                "nuget list source",
                environmentVariables: EnvironmentVariables,
                outputDataReceived: ProcessUtil.PrintMessage,
                errorDataReceived: ProcessUtil.PrintErrorMessage,
                throwOnError: false,
                cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);
        }
    }

    private void EnsureTargetRunnerConfiguration(string workingDirectory)
    {
        var defaultRunnerConfig = Path.Combine(workingDirectory, "default.runner.json");
        var xunitRunnerConfig = Path.Combine(workingDirectory, "xunit.runner.json");
        if (!File.Exists(xunitRunnerConfig) && File.Exists(defaultRunnerConfig))
        {
            File.Copy(defaultRunnerConfig, xunitRunnerConfig);
        }
    }

    private IEnumerable<string> GetTargetWorkingDirectories()
    {
        return Options.Targets
            .Select(GetTargetWorkingDirectory)
            .Distinct(GetPathComparer());
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    }

    private static string GetSanitizedAssemblyName(string target)
    {
        var fileName = Path.GetFileNameWithoutExtension(target);
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(ch => invalidCharacters.Contains(ch) ? '_' : ch).ToArray());
    }

    private static string GetTargetWorkingDirectory(string target)
    {
        var targetDirectory = Path.GetDirectoryName(target);
        if (string.IsNullOrEmpty(targetDirectory))
        {
            return Directory.GetCurrentDirectory();
        }

        return Path.GetFullPath(targetDirectory);
    }

    private static void CopyFileToUploadRoot(string sourceFile, string uploadRoot, string destinationFileName)
    {
        var destination = Path.Combine(uploadRoot, destinationFileName);
        ProcessUtil.PrintMessage($"Copying: {sourceFile} to {destination}");
        File.Copy(sourceFile, destination, overwrite: true);
    }
}
