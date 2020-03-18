// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Test.Helpers
{
    [DebuggerDisplay("{ToString(),nq}")]
    public class Project
    {
        private const string _urls = "http://127.0.0.1:0;https://127.0.0.1:0";

        public static bool IsCIEnvironment => typeof(Project).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Any(a => a.Key == "ContinuousIntegrationBuild");

        public static string ArtifactsLogDir => (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_DIR"))) 
            ? GetAssemblyMetadata("ArtifactsLogDir")
            : Path.Combine(Environment.GetEnvironmentVariable("HELIX_DIR"), "logs");
        
        public static string DotNetEfFullPath => (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DotNetEfFullPath"))) 
            ? typeof(ProjectFactoryFixture).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(attribute => attribute.Key == "DotNetEfFullPath")
                .Value
            : Environment.GetEnvironmentVariable("DotNetEfFullPath");

        public SemaphoreSlim DotNetNewLock { get; set; }
        public SemaphoreSlim NodeLock { get; set; }
        public string ProjectName { get; set; }
        public string ProjectArguments { get; set; }
        public string ProjectGuid { get; set; }
        public string TemplateOutputDir { get; set; }
        public string TargetFramework { get; set; } = GetAssemblyMetadata("Test.DefaultTargetFramework");

        public string TemplateBuildDir => Path.Combine(TemplateOutputDir, "bin", "Debug", TargetFramework);
        public string TemplatePublishDir => Path.Combine(TemplateOutputDir, "bin", "Release", TargetFramework, "publish");

        private string TemplateServerDir => Path.Combine(TemplateOutputDir, $"{ProjectName}.Server");
        private string TemplateClientDir => Path.Combine(TemplateOutputDir, $"{ProjectName}.Client");
        public string TemplateClientDebugDir => Path.Combine(TemplateClientDir, "bin", "Debug", TargetFramework);
        public string TemplateClientReleaseDir => Path.Combine(TemplateClientDir, "bin", "Release", TargetFramework, "publish");
        public string TemplateServerReleaseDir => Path.Combine(TemplateServerDir, "bin", "Release", TargetFramework, "publish");

        public ITestOutputHelper Output { get; set; }
        public IMessageSink DiagnosticsMessageSink { get; set; }

        internal async Task<ProcessEx> RunDotNetNewAsync(
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
                var execution = ProcessEx.Run(Output, AppContext.BaseDirectory, DotNetMuxer.MuxerPathOrDefault(), argString, environmentVariables);
                await execution.Exited;
                return execution;
            }
            finally
            {
                DotNetNewLock.Release();
            }
        }

        internal async Task<ProcessEx> RunDotNetPublishAsync(bool takeNodeLock = false, IDictionary<string, string> packageOptions = null, string additionalArgs = null)
        {
            Output.WriteLine("Publishing ASP.NET application...");

            // This is going to trigger a build, so we need to acquire the lock like in the other cases.
            // We want to take the node lock as some builds run NPM as part of the build and we want to make sure
            // it's run without interruptions.
            var effectiveLock = takeNodeLock ? new OrderedLock(NodeLock, DotNetNewLock) : new OrderedLock(nodeLock: null, DotNetNewLock);
            await effectiveLock.WaitAsync();
            try
            {
                var result = ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), $"publish -c Release /bl {additionalArgs}", packageOptions);
                await result.Exited;
                CaptureBinLogOnFailure(result);
                return result;
            }
            finally
            {
                effectiveLock.Release();
            }
        }

        internal async Task<ProcessEx> RunDotNetBuildAsync(bool takeNodeLock = false, IDictionary<string, string> packageOptions = null, string additionalArgs = null)
        {
            Output.WriteLine("Building ASP.NET application...");

            // This is going to trigger a build, so we need to acquire the lock like in the other cases.
            // We want to take the node lock as some builds run NPM as part of the build and we want to make sure
            // it's run without interruptions.
            var effectiveLock = takeNodeLock ? new OrderedLock(NodeLock, DotNetNewLock) : new OrderedLock(nodeLock: null, DotNetNewLock);
            await effectiveLock.WaitAsync();
            try
            {
                var result = ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), $"build -c Debug /bl {additionalArgs}", packageOptions);
                await result.Exited;
                CaptureBinLogOnFailure(result);
                return result;
            }
            finally
            {
                effectiveLock.Release();
            }
        }

        internal AspNetProcess StartBuiltServerAsync()
        {
            var environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            };

            var projectDll = Path.Combine(TemplateServerDir, $"{ProjectName}.Server.dll");
            return new AspNetProcess(Output, TemplateServerDir, projectDll, environment, published: false);
        }

        internal AspNetProcess StartBuiltClientAsync(AspNetProcess serverProcess)
        {
            var environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            };

            var projectDll = Path.Combine(TemplateClientDebugDir, $"{ProjectName}.Client.dll {serverProcess.ListeningUri.Port}");
            return new AspNetProcess(Output, TemplateOutputDir, projectDll, environment, hasListeningUri: false);
        }

        internal AspNetProcess StartPublishedServerAsync()
        {
            var environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_URLS"] = _urls,
            };

            var projectDll = $"{ProjectName}.Server.dll";
            return new AspNetProcess(Output, TemplateServerReleaseDir, projectDll, environment);
        }

        internal AspNetProcess StartPublishedClientAsync()
        {
            var environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_URLS"] = _urls,
            };

            var projectDll = $"{ProjectName}.Client.dll";
            return new AspNetProcess(Output, TemplateClientReleaseDir, projectDll, environment);
        }

        internal AspNetProcess StartBuiltProjectAsync(bool hasListeningUri = true)
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

            var projectDll = Path.Combine(TemplateBuildDir, $"{ProjectName}.dll");
            return new AspNetProcess(Output, TemplateOutputDir, projectDll, environment, hasListeningUri: hasListeningUri);
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
            return new AspNetProcess(Output, TemplatePublishDir, projectDll, environment, hasListeningUri: hasListeningUri);
        }

        internal async Task<ProcessEx> RestoreWithRetryAsync(ITestOutputHelper output, string workingDirectory)
        {
            // "npm restore" sometimes fails randomly in AppVeyor with errors like:
            //    EPERM: operation not permitted, scandir <path>...
            // This appears to be a general NPM reliability issue on Windows which has
            // been reported many times (e.g., https://github.com/npm/npm/issues/18380)
            // So, allow multiple attempts at the restore.
            const int maxAttempts = 3;
            var attemptNumber = 0;
            ProcessEx restoreResult;
            do
            {
                restoreResult = await RestoreAsync(output, workingDirectory);
                if (restoreResult.HasExited && restoreResult.ExitCode == 0)
                {
                    return restoreResult;
                }
                else
                {
                    // TODO: We should filter for EPEM here to avoid masking other errors silently.
                    output.WriteLine(
                        $"NPM restore in {workingDirectory} failed on attempt {attemptNumber} of {maxAttempts}. " +
                        $"Error was: {restoreResult.GetFormattedOutput()}");

                    // Clean up the possibly-incomplete node_modules dir before retrying
                    CleanNodeModulesFolder(workingDirectory, output);
                }
                attemptNumber++;
            } while (attemptNumber < maxAttempts);

            output.WriteLine($"Giving up attempting NPM restore in {workingDirectory} after {attemptNumber} attempts.");
            return restoreResult;

            void CleanNodeModulesFolder(string workingDirectory, ITestOutputHelper output)
            {
                var nodeModulesDir = Path.Combine(workingDirectory, "node_modules");
                try
                {
                    if (Directory.Exists(nodeModulesDir))
                    {
                        Directory.Delete(nodeModulesDir, recursive: true);
                    }
                }
                catch
                {
                    output.WriteLine($"Failed to clean up node_modules folder at {nodeModulesDir}.");
                }
            }
        }

        private async Task<ProcessEx> RestoreAsync(ITestOutputHelper output, string workingDirectory)
        {
            // It's not safe to run multiple NPM installs in parallel
            // https://github.com/npm/npm/issues/2500
            await NodeLock.WaitAsync();
            try
            {
                output.WriteLine($"Restoring NPM packages in '{workingDirectory}' using npm...");
                var result = ProcessEx.RunViaShell(output, workingDirectory, "npm install");
                return result;
            }
            finally
            {
                NodeLock.Release();
            }
        }

        internal async Task<ProcessEx> RunDotNetEfCreateMigrationAsync(string migrationName)
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
                
                var result = ProcessEx.Run(Output, TemplateOutputDir, command, args);
                await result.Exited;
                return result;
            }
            finally
            {
                DotNetNewLock.Release();
            }
        }

        internal async Task<ProcessEx> RunDotNetEfUpdateDatabaseAsync()
        {
            var args = "--verbose --no-build database update";

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
                
                var result = ProcessEx.Run(Output, TemplateOutputDir, command, args);
                await result.Exited;
                return result;
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

            public OrderedLock(SemaphoreSlim nodeLock, SemaphoreSlim dotnetLock)
            {
                NodeLock = nodeLock;
                DotnetLock = dotnetLock;
            }

            public SemaphoreSlim NodeLock { get; }
            public SemaphoreSlim DotnetLock { get; }

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
