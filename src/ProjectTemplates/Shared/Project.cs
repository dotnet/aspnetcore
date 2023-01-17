// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Templates.Test.Helpers.ProcessLock;

namespace Templates.Test.Helpers;

[DebuggerDisplay("{ToString(),nq}")]
public class Project : IDisposable
{
    private const string _urls = "http://127.0.0.1:0;https://127.0.0.1:0";

    public static string ArtifactsLogDir
    {
        get
        {
            var helixWorkItemUploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (!string.IsNullOrEmpty(helixWorkItemUploadRoot))
            {
                return helixWorkItemUploadRoot;
            }

            var testLogFolder = typeof(Project).Assembly.GetCustomAttribute<TestFrameworkFileLoggerAttribute>()?.BaseDirectory;
            if (string.IsNullOrEmpty(testLogFolder))
            {
                throw new InvalidOperationException($"No test log folder specified via {nameof(TestFrameworkFileLoggerAttribute)}.");
            }
            return testLogFolder;
        }
    }

    public static string DotNetEfFullPath => (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DotNetEfFullPath")))
        ? typeof(ProjectFactoryFixture).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(attribute => attribute.Key == "DotNetEfFullPath")
            .Value
        : Environment.GetEnvironmentVariable("DotNetEfFullPath");

    public string ProjectName { get; set; }
    public string ProjectArguments { get; set; }
    public string ProjectGuid { get; set; }
    public string TemplateOutputDir { get; set; }
    public string TargetFramework { get; set; } = GetAssemblyMetadata("Test.DefaultTargetFramework");
    public string RuntimeIdentifier { get; set; } = string.Empty;
    public static DevelopmentCertificate DevCert { get; } = DevelopmentCertificate.Create(AppContext.BaseDirectory);

    public string TemplateBuildDir => Path.Combine(TemplateOutputDir, "bin", "Debug", TargetFramework, RuntimeIdentifier);
    public string TemplatePublishDir => Path.Combine(TemplateOutputDir, "bin", "Release", TargetFramework, RuntimeIdentifier, "publish");

    public ITestOutputHelper Output { get; set; }
    public IMessageSink DiagnosticsMessageSink { get; set; }

    internal async Task RunDotNetNewAsync(
        string templateName,
        string auth = null,
        string language = null,
        bool useLocalDB = false,
        bool noHttps = false,
        bool errorOnRestoreError = true,
        string[] args = null,
        // Used to set special options in MSBuild
        IDictionary<string, string> environmentVariables = null)
    {
        var hiveArg = $" --debug:disable-sdk-templates --debug:custom-hive \"{TemplatePackageInstaller.CustomHivePath}\"";
        var argString = $"new {templateName} {hiveArg}";
        environmentVariables ??= new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(auth))
        {
            argString += $" --auth {auth}";
        }

        if (!string.IsNullOrEmpty(language))
        {
            argString += $" -lang {language}";
        }

        if (useLocalDB)
        {
            argString += $" --use-local-db";
        }

        if (noHttps)
        {
            argString += $" --no-https";
        }

        if (args != null)
        {
            foreach (var arg in args)
            {
                argString += " " + arg;
            }
        }

        // Save a copy of the arguments used for better diagnostic error messages later.
        // We omit the hive argument and the template output dir as they are not relevant and add noise.
        ProjectArguments = argString.Replace(hiveArg, "");

        argString += $" -o {TemplateOutputDir}";

        if (Directory.Exists(TemplateOutputDir))
        {
            Output.WriteLine($"Template directory already exists, deleting contents of {TemplateOutputDir}");
            Directory.Delete(TemplateOutputDir, recursive: true);
        }

        using var execution = ProcessEx.Run(Output, AppContext.BaseDirectory, DotNetMuxer.MuxerPathOrDefault(), argString, environmentVariables);
        await execution.Exited;

        var result = new ProcessResult(execution);

        // Because dotnet new automatically restores but silently ignores restore errors, need to handle restore errors explicitly
        if (errorOnRestoreError && (execution.Output.Contains("Restore failed.") || execution.Error.Contains("Restore failed.")))
        {
            result.ExitCode = -1;
        }

        Assert.True(0 == result.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", this, result));
    }

    internal async Task RunDotNetPublishAsync(IDictionary<string, string> packageOptions = null, string additionalArgs = null, bool noRestore = true)
    {
        Output.WriteLine("Publishing ASP.NET Core application...");

        // Avoid restoring as part of build or publish. These projects should have already restored as part of running dotnet new. Explicitly disabling restore
        // should avoid any global contention and we can execute a build or publish in a lock-free way

        var restoreArgs = noRestore ? "--no-restore" : null;

        using var execution = ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), $"publish {restoreArgs} -c Release /bl {additionalArgs}", packageOptions);
        await execution.Exited;

        var result = new ProcessResult(execution);

        // Fail if there were build warnings
        if (execution.Output.Contains(": warning") || execution.Error.Contains(": warning"))
        {
            result.ExitCode = -1;
        }

        CaptureBinLogOnFailure(execution);

        Assert.True(0 == result.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", this, result));
    }

    internal async Task RunDotNetBuildAsync(IDictionary<string, string> packageOptions = null, string additionalArgs = null, bool errorOnBuildWarning = true)
    {
        Output.WriteLine("Building ASP.NET Core application...");

        // Avoid restoring as part of build or publish. These projects should have already restored as part of running dotnet new. Explicitly disabling restore
        // should avoid any global contention and we can execute a build or publish in a lock-free way

        using var execution = ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), $"build --no-restore -c Debug /bl {additionalArgs}", packageOptions);
        await execution.Exited;

        var result = new ProcessResult(execution);

        // Fail if there were build warnings
        if (errorOnBuildWarning && (execution.Output.Contains(": warning") || execution.Error.Contains(": warning")))
        {
            result.ExitCode = -1;
        }

        CaptureBinLogOnFailure(execution);

        Assert.True(0 == result.ExitCode, ErrorMessages.GetFailedProcessMessage("build", this, result));
    }

    internal AspNetProcess StartBuiltProjectAsync(bool hasListeningUri = true, ILogger logger = null)
    {
        var environment = new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = _urls,
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["ASPNETCORE_Logging__Console__LogLevel__Default"] = "Debug",
            ["ASPNETCORE_Logging__Console__LogLevel__System"] = "Debug",
            ["ASPNETCORE_Logging__Console__LogLevel__Microsoft"] = "Debug",
            ["ASPNETCORE_Logging__Console__FormatterOptions__IncludeScopes"] = "true",
        };

        var projectDll = Path.Combine(TemplateBuildDir, $"{ProjectName}.dll");
        return new AspNetProcess(DevCert, Output, TemplateOutputDir, projectDll, environment, published: false, hasListeningUri: hasListeningUri, logger: logger);
    }

    internal AspNetProcess StartPublishedProjectAsync(bool hasListeningUri = true, bool usePublishedAppHost = false)
    {
        var environment = new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = _urls,
            ["ASPNETCORE_Logging__Console__LogLevel__Default"] = "Debug",
            ["ASPNETCORE_Logging__Console__LogLevel__System"] = "Debug",
            ["ASPNETCORE_Logging__Console__LogLevel__Microsoft"] = "Debug",
            ["ASPNETCORE_Logging__Console__FormatterOptions__IncludeScopes"] = "true",
        };

        var projectDll = Path.Combine(TemplatePublishDir, $"{ProjectName}.dll");
        return new AspNetProcess(DevCert, Output, TemplatePublishDir, projectDll, environment, published: true, hasListeningUri: hasListeningUri, usePublishedAppHost: usePublishedAppHost);
    }

    internal async Task RunDotNetEfCreateMigrationAsync(string migrationName)
    {
        var args = $"--verbose --no-build migrations add {migrationName}";

        var command = DotNetMuxer.MuxerPathOrDefault();
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DotNetEfFullPath")))
        {
            args = $"\"{DotNetEfFullPath}\" " + args;
        }
        else
        {
            command = "dotnet-ef";
        }

        using var result = ProcessEx.Run(Output, TemplateOutputDir, command, args);
        await result.Exited;
        var processResult = new ProcessResult(result);
        Assert.True(0 == processResult.ExitCode, ErrorMessages.GetFailedProcessMessage("run EF migrations", this, processResult));
    }

    internal async Task RunDotNetEfUpdateDatabaseAsync()
    {
        var args = "--verbose --no-build database update";

        var command = DotNetMuxer.MuxerPathOrDefault();
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DotNetEfFullPath")))
        {
            args = $"\"{DotNetEfFullPath}\" " + args;
        }
        else
        {
            command = "dotnet-ef";
        }

        using var result = ProcessEx.Run(Output, TemplateOutputDir, command, args);
        await result.Exited;
        var processResult = new ProcessResult(result);
        Assert.True(0 == processResult.ExitCode, ErrorMessages.GetFailedProcessMessage("update database", this, processResult));
    }

    // If this fails, you should generate new migrations via migrations/updateMigrations.cmd
    public void AssertEmptyMigration(string migration)
    {
        var fullPath = Path.Combine(TemplateOutputDir, "Data/Migrations");
        var file = Directory.EnumerateFiles(fullPath).Where(f => f.EndsWith($"{migration}.cs", StringComparison.Ordinal)).FirstOrDefault();

        Assert.NotNull(file);
        var contents = File.ReadAllText(file);

        var emptyMigration = @"/// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }";

        // This comparison can break depending on how GIT checked out newlines on different files.
        Assert.Contains(RemoveNewLines(emptyMigration), RemoveNewLines(contents));

        static string RemoveNewLines(string str)
        {
            return str.Replace("\n", string.Empty).Replace("\r", string.Empty);
        }
    }

    public void AssertFileExists(string path, bool shouldExist)
    {
        var fullPath = Path.Combine(TemplateOutputDir, path);
        var doesExist = File.Exists(fullPath);

        if (shouldExist)
        {
            Assert.True(doesExist, "Expected file to exist, but it doesn't: " + path);
        }
        else
        {
            Assert.False(doesExist, "Expected file not to exist, but it does: " + path);
        }
    }

    public async Task VerifyLaunchSettings(string[] expectedLaunchProfileNames)
    {
        var launchSettingsFiles = Directory.EnumerateFiles(TemplateOutputDir, "launchSettings.json", SearchOption.AllDirectories);

        foreach (var filePath in launchSettingsFiles)
        {
            using var launchSettingsFile = File.OpenRead(filePath);
            using var launchSettings = await JsonDocument.ParseAsync(launchSettingsFile);

            var profiles = launchSettings.RootElement.GetProperty("profiles");
            var profilesEnumerator = profiles.EnumerateObject().GetEnumerator();

            foreach (var expectedProfileName in expectedLaunchProfileNames)
            {
                Assert.True(profilesEnumerator.MoveNext());

                var actualProfile = profilesEnumerator.Current;

                // Launch profile names are case sensitive
                Assert.Equal(expectedProfileName, actualProfile.Name, StringComparer.Ordinal);

                if (actualProfile.Value.GetProperty("commandName").GetString() == "Project")
                {
                    var applicationUrl = actualProfile.Value.GetProperty("applicationUrl");
                    if (string.Equals(expectedProfileName, "http", StringComparison.Ordinal))
                    {
                        Assert.DoesNotContain("https://", applicationUrl.GetString());
                    }

                    if (string.Equals(expectedProfileName, "https", StringComparison.Ordinal))
                    {
                        Assert.StartsWith("https://", applicationUrl.GetString());
                    }
                }
            }

            // Check there are no more launch profiles defined
            Assert.False(profilesEnumerator.MoveNext());

            if (launchSettings.RootElement.TryGetProperty("iisSettings", out var iisSettings)
                && iisSettings.TryGetProperty("iisExpress", out var iisExpressSettings))
            {
                var iisSslPort = iisExpressSettings.GetProperty("sslPort").GetInt32();
                if (expectedLaunchProfileNames.Contains("https"))
                {
                    Assert.True(iisSslPort >= 44300 && iisSslPort <= 44399, $"IIS Express port was expected to be >= 44300 and <= 44399 but was {iisSslPort} in file {filePath}");
                }
                else
                {
                    Assert.Equal(0, iisSslPort);
                }
            }
        }
    }

    public string ReadFile(string path)
    {
        AssertFileExists(path, shouldExist: true);
        return File.ReadAllText(Path.Combine(TemplateOutputDir, path));
    }

    internal async Task RunDotNetNewRawAsync(string arguments)
    {
        var result = ProcessEx.Run(
            Output,
            AppContext.BaseDirectory,
            DotNetMuxer.MuxerPathOrDefault(),
            arguments +
                $" --debug:disable-sdk-templates --debug:custom-hive \"{TemplatePackageInstaller.CustomHivePath}\"" +
                $" -o {TemplateOutputDir}");
        await result.Exited;
        Assert.True(result.ExitCode == 0, result.GetFormattedOutput());
    }

    public void Dispose()
    {
        DeleteOutputDirectory();
    }

    public void DeleteOutputDirectory()
    {
        const int NumAttempts = 10;

        for (var numAttemptsRemaining = NumAttempts; numAttemptsRemaining > 0; numAttemptsRemaining--)
        {
            try
            {
                Directory.Delete(TemplateOutputDir, true);
                return;
            }
            catch (Exception ex)
            {
                if (numAttemptsRemaining > 1)
                {
                    DiagnosticsMessageSink.OnMessage(new DiagnosticMessage($"Failed to delete directory {TemplateOutputDir} because of error {ex.Message}. Will try again {numAttemptsRemaining - 1} more time(s)."));
                    Thread.Sleep(3000);
                }
                else
                {
                    DiagnosticsMessageSink.OnMessage(new DiagnosticMessage($"Giving up trying to delete directory {TemplateOutputDir} after {NumAttempts} attempts. Most recent error was: {ex.StackTrace}"));
                }
            }
        }
    }

    private sealed class OrderedLock
    {
        private bool _nodeLockTaken;
        private bool _dotNetLockTaken;

        public OrderedLock(ProcessLock nodeLock, ProcessLock dotnetLock)
        {
            NodeLock = nodeLock;
            DotnetLock = dotnetLock;
        }

        public ProcessLock NodeLock { get; }
        public ProcessLock DotnetLock { get; }

        public async Task WaitAsync()
        {
            if (NodeLock == null)
            {
                await DotnetLock.WaitAsync();
                _dotNetLockTaken = true;
                return;
            }

            try
            {
                // We want to take the NPM lock first as is going to be the busiest one, and we want other threads to be
                // able to run dotnet new while we are waiting for another thread to finish running NPM.
                await NodeLock.WaitAsync();
                _nodeLockTaken = true;
                await DotnetLock.WaitAsync();
                _dotNetLockTaken = true;
            }
            catch
            {
                if (_nodeLockTaken)
                {
                    NodeLock.Release();
                    _nodeLockTaken = false;
                }
                throw;
            }
        }

        public void Release()
        {
            try
            {
                if (_dotNetLockTaken)
                {

                    DotnetLock.Release();
                    _dotNetLockTaken = false;
                }
            }
            finally
            {
                if (_nodeLockTaken)
                {
                    NodeLock.Release();
                    _nodeLockTaken = false;
                }
            }
        }
    }

    private void CaptureBinLogOnFailure(ProcessEx result)
    {
        if (result.ExitCode != 0 && !string.IsNullOrEmpty(ArtifactsLogDir))
        {
            var sourceFile = Path.Combine(TemplateOutputDir, "msbuild.binlog");
            Assert.True(File.Exists(sourceFile), $"Log for '{ProjectName}' not found in '{sourceFile}'. Execution output: {result.Output}");
            var destination = Path.Combine(ArtifactsLogDir, ProjectName + ".binlog");
            File.Move(sourceFile, destination, overwrite: true); // binlog will exist on retries
        }
    }

    public override string ToString() => $"{ProjectName}: {TemplateOutputDir}";

    private static string GetAssemblyMetadata(string key)
    {
        var attribute = typeof(Project).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key);

        if (attribute is null)
        {
            throw new ArgumentException($"AssemblyMetadataAttribute with key {key} was not found.");
        }

        return attribute.Value;
    }
}
