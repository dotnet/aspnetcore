// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Templates.Test.Helpers.ProcessLock;

namespace Templates.Test.Helpers
{
    [DebuggerDisplay("{ToString(),nq}")]
    public class Project : IDisposable
    {
        private const string _urls = "http://127.0.0.1:0;https://127.0.0.1:0";

        public static string ArtifactsLogDir => (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT")))
            ? GetAssemblyMetadata("ArtifactsLogDir")
            : Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");

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

        public string TemplateBuildDir => Path.Combine(TemplateOutputDir, "bin", "Debug", TargetFramework, RuntimeIdentifier);
        public string TemplatePublishDir => Path.Combine(TemplateOutputDir, "bin", "Release", TargetFramework, RuntimeIdentifier, "publish");

        public ITestOutputHelper Output { get; set; }
        public IMessageSink DiagnosticsMessageSink { get; set; }

        internal async Task<ProcessResult> RunDotNetNewAsync(
            string templateName,
            string auth = null,
            string language = null,
            bool useLocalDB = false,
            bool noHttps = false,
            string[] args = null,
            // Used to set special options in MSBuild
            IDictionary<string, string> environmentVariables = null)
        {
            var hiveArg = $"--debug:custom-hive \"{TemplatePackageInstaller.CustomHivePath}\"";
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

            // Only run one instance of 'dotnet new' at once, as a workaround for
            // https://github.com/aspnet/templating/issues/63

            await DotNetNewLock.WaitAsync();
            try
            {
                using var execution = ProcessEx.Run(Output, AppContext.BaseDirectory, DotNetMuxer.MuxerPathOrDefault(), argString, environmentVariables);
                await execution.Exited;
                return new ProcessResult(execution);
            }
            finally
            {
                DotNetNewLock.Release();
            }
        }

        internal async Task<ProcessResult> RunDotNetPublishAsync(IDictionary<string, string> packageOptions = null, string additionalArgs = null)
        {
            Output.WriteLine("Publishing ASP.NET Core application...");

            // Avoid restoring as part of build or publish. These projects should have already restored as part of running dotnet new. Explicitly disabling restore
            // should avoid any global contention and we can execute a build or publish in a lock-free way

            using var result = ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), $"publish --no-restore -c Release /bl {additionalArgs}", packageOptions);
            await result.Exited;
            CaptureBinLogOnFailure(result);
            return new ProcessResult(result);
        }

        internal async Task<ProcessResult> RunDotNetBuildAsync(IDictionary<string, string> packageOptions = null, string additionalArgs = null)
        {
            Output.WriteLine("Building ASP.NET Core application...");

            // Avoid restoring as part of build or publish. These projects should have already restored as part of running dotnet new. Explicitly disabling restore
            // should avoid any global contention and we can execute a build or publish in a lock-free way

            using var result = ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), $"build --no-restore -c Debug /bl {additionalArgs}", packageOptions);
            await result.Exited;
            CaptureBinLogOnFailure(result);
            return new ProcessResult(result);
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
                ["ASPNETCORE_Logging__Console__IncludeScopes"] = "true",
            };

            var launchSettingsJson = Path.Combine(TemplateOutputDir, "Properties", "launchSettings.json");
            if (File.Exists(launchSettingsJson))
            {
                // When executing "dotnet run", the launch urls specified in the app's launchSettings.json have higher precedence
                // than ambient environment variables. When present, we have to edit this file to allow the application to pick random ports.
                var original = File.ReadAllText(launchSettingsJson);
                var updated = original.Replace(
                    "\"applicationUrl\": \"https://localhost:5001;http://localhost:5000\"",
                    $"\"applicationUrl\": \"{_urls}\"");

                if (updated == original)
                {
                    Output.WriteLine("applicationUrl is not specified in launchSettings.json");
                }
                else
                {
                    Output.WriteLine("Updating applicationUrl in launchSettings.json");
                    File.WriteAllText(launchSettingsJson, updated);
                }
            }
            else
            {
                Output.WriteLine("No launchSettings.json found to update.");
            }

            var projectDll = Path.Combine(TemplateBuildDir, $"{ProjectName}.dll");
            return new AspNetProcess(Output, TemplateOutputDir, projectDll, environment, published: false, hasListeningUri: hasListeningUri, logger: logger);
        }

        internal AspNetProcess StartPublishedProjectAsync(bool hasListeningUri = true)
        {
            var environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_URLS"] = _urls,
                ["ASPNETCORE_Logging__Console__LogLevel__Default"] = "Debug",
                ["ASPNETCORE_Logging__Console__LogLevel__System"] = "Debug",
                ["ASPNETCORE_Logging__Console__LogLevel__Microsoft"] = "Debug",
                ["ASPNETCORE_Logging__Console__IncludeScopes"] = "true",
            };

            var projectDll = $"{ProjectName}.dll";
            return new AspNetProcess(Output, TemplatePublishDir, projectDll, environment, published: true, hasListeningUri: hasListeningUri);
        }

        internal async Task<ProcessResult> RunDotNetEfCreateMigrationAsync(string migrationName)
        {
            var args = $"--verbose --no-build migrations add {migrationName}";

            // Only run one instance of 'dotnet new' at once, as a workaround for
            // https://github.com/aspnet/templating/issues/63
            await DotNetNewLock.WaitAsync();
            try
            {
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
                return new ProcessResult(result);
            }
            finally
            {
                DotNetNewLock.Release();
            }
        }

        internal async Task<ProcessResult> RunDotNetEfUpdateDatabaseAsync()
        {
            var assembly = typeof(ProjectFactoryFixture).Assembly;

            var dotNetEfFullPath = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(attribute => attribute.Key == "DotNetEfFullPath")
                .Value;

            var args = $"\"{dotNetEfFullPath}\" --verbose --no-build database update";

            // Only run one instance of 'dotnet new' at once, as a workaround for
            // https://github.com/aspnet/templating/issues/63
            await DotNetNewLock.WaitAsync();
            try
            {
                using var result = ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), args);
                await result.Exited;
                return new ProcessResult(result);
            }
            finally
            {
                DotNetNewLock.Release();
            }
        }

        // If this fails, you should generate new migrations via migrations/updateMigrations.cmd
        public void AssertEmptyMigration(string migration)
        {
            var fullPath = Path.Combine(TemplateOutputDir, "Data/Migrations");
            var file = Directory.EnumerateFiles(fullPath).Where(f => f.EndsWith($"{migration}.cs")).FirstOrDefault();

            Assert.NotNull(file);
            var contents = File.ReadAllText(file);

            var emptyMigration = @"protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

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

        public string ReadFile(string path)
        {
            AssertFileExists(path, shouldExist: true);
            return File.ReadAllText(Path.Combine(TemplateOutputDir, path));
        }

        internal async Task<ProcessEx> RunDotNetNewRawAsync(string arguments)
        {
            await DotNetNewLock.WaitAsync();
            try
            {
                var result = ProcessEx.Run(
                    Output,
                    AppContext.BaseDirectory,
                    DotNetMuxer.MuxerPathOrDefault(),
                    arguments +
                        $" --debug:custom-hive \"{TemplatePackageInstaller.CustomHivePath}\"" +
                        $" -o {TemplateOutputDir}");
                await result.Exited;
                return result;
            }
            finally
            {
                DotNetNewLock.Release();
            }
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

        private class OrderedLock
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
                Assert.True(File.Exists(sourceFile), $"Log for '{ProjectName}' not found in '{sourceFile}'.");
                var destination = Path.Combine(ArtifactsLogDir, ProjectName + ".binlog");
                File.Move(sourceFile, destination);
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
}
