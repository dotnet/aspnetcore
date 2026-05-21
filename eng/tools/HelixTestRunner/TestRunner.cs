// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                ProcessUtil.PrintMessage("Skipping setting PLAYWRIGHT_BROWSERS_PATH");
            }

            ProcessUtil.PrintMessage($"Creating nuget restore directory: {nugetRestore}");
            Directory.CreateDirectory(nugetRestore);

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
            foreach (var target in Options.Targets)
            {
                ProcessUtil.PrintMessage($"Running test discovery for assembly: {target}");
                var workingDirectory = GetTargetWorkingDirectory(target);
                EnsureTargetRunnerConfiguration(workingDirectory);

                // Run test discovery so we know if there are tests to run.
                var discoveryResult = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                    $"vstest \"{Path.GetFileName(target)}\" -lt",
                    workingDirectory: workingDirectory,
                    environmentVariables: EnvironmentVariables,
                    cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

                if (discoveryResult.StandardOutput.Contains("Exception thrown", StringComparison.Ordinal) ||
                    discoveryResult.StandardError.Contains("Exception thrown", StringComparison.Ordinal))
                {
                    ProcessUtil.PrintMessage($"Exception thrown during test discovery for {target}.");
                    ProcessUtil.PrintMessage(discoveryResult.StandardOutput);
                    ProcessUtil.PrintMessage(discoveryResult.StandardError);
                    return false;
                }
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
            var testProcessTimeout = Options.Timeout.Subtract(TimeSpan.FromMinutes(5));
            if (testProcessTimeout <= TimeSpan.Zero)
            {
                testProcessTimeout = Options.Timeout;
            }

            var deadline = DateTime.UtcNow + testProcessTimeout;
            var uploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT") ?? Directory.GetCurrentDirectory();

            foreach (var target in Options.Targets)
            {
                ProcessUtil.PrintMessage($"Running tests for assembly: {target}");
                var remainingTime = deadline - DateTime.UtcNow;
                if (remainingTime <= TimeSpan.Zero)
                {
                    ProcessUtil.PrintMessage($"Helix test batch exceeded configured timeout: {testProcessTimeout.TotalMinutes}m.");
                    exitCode = exitCode == 0 ? 1 : exitCode;
                    break;
                }

                var workingDirectory = GetTargetWorkingDirectory(target);
                EnsureTargetRunnerConfiguration(workingDirectory);
                var assemblyName = GetSanitizedAssemblyName(target);
                var diagLog = Path.Combine(uploadRoot, $"vstest_{assemblyName}.log");
                var commonTestArgs = $"test \"{Path.GetFileName(target)}\" --diag:\"{diagLog}\" --logger xunit --logger \"console;verbosity=normal\" " +
                                     "--blame-crash --blame-hang-timeout 15m";
                using var cts = new CancellationTokenSource(remainingTime);

                if (Options.Quarantined)
                {
                    ProcessUtil.PrintMessage("Running quarantined tests.");

                    // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                    var result = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                        commonTestArgs + " --filter \"Quarantined=true\"",
                        workingDirectory: workingDirectory,
                        environmentVariables: EnvironmentVariables,
                        outputDataReceived: ProcessUtil.PrintMessage,
                        errorDataReceived: ProcessUtil.PrintErrorMessage,
                        throwOnError: false,
                        cancellationToken: cts.Token);

                    if (cts.Token.IsCancellationRequested)
                    {
                        ProcessUtil.PrintMessage($"Quarantined tests for {target} exceeded configured timeout: {remainingTime.TotalMinutes}m.");
                        exitCode = exitCode == 0 ? 1 : exitCode;
                        break;
                    }
                    if (result.ExitCode != 0)
                    {
                        ProcessUtil.PrintMessage($"Failure in quarantined tests for {target}. Exit code: {result.ExitCode}.");
                        exitCode = exitCode == 0 ? result.ExitCode : exitCode;
                    }
                }
                else
                {
                    ProcessUtil.PrintMessage("Running non-quarantined tests.");

                    // Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
                    var result = await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                        commonTestArgs + " --filter \"Quarantined!=true|Quarantined=false\"",
                        workingDirectory: workingDirectory,
                        environmentVariables: EnvironmentVariables,
                        outputDataReceived: ProcessUtil.PrintMessage,
                        errorDataReceived: ProcessUtil.PrintErrorMessage,
                        throwOnError: false,
                        cancellationToken: cts.Token);

                    if (cts.Token.IsCancellationRequested)
                    {
                        ProcessUtil.PrintMessage($"Non-quarantined tests for {target} exceeded configured timeout: {remainingTime.TotalMinutes}m.");
                        exitCode = exitCode == 0 ? 1 : exitCode;
                        break;
                    }
                    if (result.ExitCode != 0)
                    {
                        ProcessUtil.PrintMessage($"Failure in non-quarantined tests for {target}. Exit code: {result.ExitCode}.");
                        exitCode = exitCode == 0 ? result.ExitCode : exitCode;
                    }
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
        ProcessUtil.PrintMessage("Trying to upload results...");

        var uploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
        var copiedResultFiles = new List<string>();

        foreach (var target in Options.Targets)
        {
            var workingDirectory = GetTargetWorkingDirectory(target);
            var assemblyName = GetSanitizedAssemblyName(target);
            var resultPath = Path.Combine(workingDirectory, "TestResults", "TestResults.xml");
            if (!File.Exists(resultPath))
            {
                continue;
            }

            var destination = Path.Combine(Directory.GetCurrentDirectory(), $"testResults_{assemblyName}.xml");
            ProcessUtil.PrintMessage($"Copying {resultPath} to {destination}");
            File.Copy(resultPath, destination, overwrite: true);
            copiedResultFiles.Add(destination);
        }

        if (copiedResultFiles.Count == 0)
        {
            ProcessUtil.PrintMessage("No test results found.");
        }
        else if (copiedResultFiles.Count == 1)
        {
            ProcessUtil.PrintMessage($"Copying {copiedResultFiles[0]} to ./testResults.xml");
            File.Copy(copiedResultFiles[0], "testResults.xml", overwrite: true);
        }
        else
        {
            ProcessUtil.PrintMessage("Merging batched test results into ./testResults.xml");
            MergeTestResults(copiedResultFiles, "testResults.xml");
        }

        if (string.IsNullOrEmpty(uploadRoot))
        {
            ProcessUtil.PrintMessage("No HELIX_WORKITEM_UPLOAD_ROOT specified, skipping upload root copies");
            return;
        }

        if (File.Exists("testResults.xml"))
        {
            CopyFileToUploadRoot("testResults.xml", uploadRoot, "testResults.xml");
        }

        foreach (var resultFile in copiedResultFiles)
        {
            CopyFileToUploadRoot(resultFile, uploadRoot, Path.GetFileName(resultFile));
        }

        foreach (var target in Options.Targets)
        {
            var workingDirectory = GetTargetWorkingDirectory(target);
            var assemblyName = GetSanitizedAssemblyName(target);
            var artifactsLogDirectory = Path.Combine(workingDirectory, "artifacts", "log");
            if (Directory.Exists(artifactsLogDirectory))
            {
                ProcessUtil.PrintMessage($"Copying logs from {artifactsLogDirectory} to {uploadRoot}/");
                foreach (var file in Directory.EnumerateFiles(artifactsLogDirectory, "*.log", SearchOption.AllDirectories))
                {
                    var logDirectoryName = Path.GetFileName(Path.GetDirectoryName(file));
                    var logName = $"{assemblyName}_{logDirectoryName}_{Path.GetFileName(file)}";
                    CopyFileToUploadRoot(file, uploadRoot, logName);
                }
            }
            else
            {
                ProcessUtil.PrintMessage($"No logs found in {artifactsLogDirectory}");
            }

            var resultsDirectory = Path.Combine(workingDirectory, "TestResults");
            if (Directory.Exists(resultsDirectory))
            {
                ProcessUtil.PrintMessage($"Copying {resultsDirectory}/Sequence*.xml to {uploadRoot}/");
                foreach (var file in Directory.EnumerateFiles(resultsDirectory, "Sequence*.xml", SearchOption.AllDirectories))
                {
                    var fileName = $"{assemblyName}_{Path.GetFileName(file)}";
                    CopyFileToUploadRoot(file, uploadRoot, fileName);
                }
            }
            else
            {
                ProcessUtil.PrintMessage($"No TestResults directory found for {target}.");
            }
        }
    }

    private async Task AddHelixSourcesAsync()
    {
        var configDirectories = new HashSet<string>(GetTargetWorkingDirectories(), GetPathComparer())
        {
            Directory.GetCurrentDirectory(),
        };

        foreach (var directory in configDirectories)
        {
            var nugetConfigPath = Path.Combine(directory, "NuGet.config");
            if (!File.Exists(nugetConfigPath))
            {
                ProcessUtil.PrintMessage($"No NuGet.config found in {directory}, skipping source update.");
                continue;
            }

            ProcessUtil.PrintMessage($"Adding current directory to nuget sources: {Options.HELIX_WORKITEM_ROOT} using {nugetConfigPath}");
            await ProcessUtil.RunAsync($"{Options.DotnetRoot}/dotnet",
                $"nuget add source \"{Options.HELIX_WORKITEM_ROOT}\" --configfile \"{nugetConfigPath}\"",
                workingDirectory: directory,
                environmentVariables: EnvironmentVariables,
                outputDataReceived: ProcessUtil.PrintMessage,
                errorDataReceived: ProcessUtil.PrintErrorMessage,
                throwOnError: false,
                cancellationToken: new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);
        }

        if (File.Exists("NuGet.config"))
        {
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

    private static void MergeTestResults(IEnumerable<string> resultFiles, string destination)
    {
        var root = new XElement("assemblies");
        foreach (var resultFile in resultFiles)
        {
            var document = XDocument.Load(resultFile);
            if (document.Root is null)
            {
                continue;
            }

            if (document.Root.Name.LocalName.Equals("assemblies", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var assembly in document.Root.Elements())
                {
                    root.Add(new XElement(assembly));
                }
            }
            else if (document.Root.Name.LocalName.Equals("assembly", StringComparison.OrdinalIgnoreCase))
            {
                root.Add(new XElement(document.Root));
            }
        }

        new XDocument(root).Save(destination);
    }
}
