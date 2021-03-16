// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public abstract class BlazorTemplateTest : IAsyncLifetime
    {
        public const int BUILDCREATEPUBLISH_PRIORITY = -1000;

        public BlazorTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Output = output;
        }

        public async Task InitializeAsync()
        {
            var sink = new TestSink();
            sink.MessageLogged += LogBrowserManagerMessage;
            var factory = new TestLoggerFactory(sink, enabled: true);
            BrowserManager = await BrowserManager.CreateAsync(CreateConfiguration(), factory);
            BrowserContextInfo = new ContextInformation(CreateFactory(Output));
        }

        private static IConfiguration CreateConfiguration()
        {
            var basePath = Path.GetDirectoryName(typeof(BlazorTemplateTest).Assembly.Location);
            var os = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => "win",
                PlatformID.Unix => "linux",
                PlatformID.MacOSX => "osx",
                _ => null
            };

            var builder = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(basePath, "playwrightSettings.json"))
                .AddJsonFile(Path.Combine(basePath, $"playwrightSettings.{os}.json"), optional: true);

            if (_isCIEnvironment)
            {
                builder.AddJsonFile(Path.Combine(basePath, "playwrightSettings.ci.json"), optional: true)
                    .AddJsonFile(Path.Combine(basePath, $"playwrightSettings.ci.{os}.json"), optional: true);
            }

            if (Debugger.IsAttached)
            {
                builder.AddJsonFile(Path.Combine(basePath, "playwrightSettings.debug.json"), optional: true);
            }

            return builder.Build();
        }

        public Task DisposeAsync() => BrowserManager.DisposeAsync();

        public ProjectFactoryFixture ProjectFactory { get; set; }
        public ITestOutputHelper Output { get; }
        public ContextInformation BrowserContextInfo { get; protected set; }
        public BrowserManager BrowserManager { get; private set; }

        public abstract string ProjectType { get; }
        private static readonly bool _isCIEnvironment =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ContinuousIntegrationBuild"));


        private void LogBrowserManagerMessage(WriteContext context)
        {
            Output.WriteLine(context.Message);
        }

        public static ILoggerFactory CreateFactory(ITestOutputHelper output)
        {
            var testSink = new TestSink();
            testSink.MessageLogged += LogMessage;
            var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
            return loggerFactory;

            void LogMessage(WriteContext ctx)
            {
                output.WriteLine($"{MapLogLevel(ctx)}: [Browser]{ctx.Message}");

                static string MapLogLevel(WriteContext obj) => obj.LogLevel switch
                {
                    LogLevel.Trace => "trace",
                    LogLevel.Debug => "dbug",
                    LogLevel.Information => "info",
                    LogLevel.Warning => "warn",
                    LogLevel.Error => "error",
                    LogLevel.Critical => "crit",
                    LogLevel.None => "info",
                    _ => "info"
                };
            }
        }

        protected async Task<Project> CreateBuildPublishAsync(string projectName, string auth = null, string[] args = null, string targetFramework = null, bool serverProject = false, bool onlyCreate = false)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            var project = await ProjectFactory.GetOrCreateProject(projectName, Output);
            if (targetFramework != null)
            {
                project.TargetFramework = targetFramework;
            }

            var createResult = await project.RunDotNetNewAsync(ProjectType, auth: auth, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            if (!onlyCreate)
            {
                var targetProject = project;
                if (serverProject)
                {
                    targetProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");
                }

                var publishResult = await targetProject.RunDotNetPublishAsync(noRestore: !serverProject);
                Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", targetProject, publishResult));

                // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
                // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
                // later, while the opposite is not true.

                var buildResult = await targetProject.RunDotNetBuildAsync();
                Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", targetProject, buildResult));
            }

            return project;
        }

        protected static Project GetSubProject(Project project, string projectDirectory, string projectName)
        {
            var subProjectDirectory = Path.Combine(project.TemplateOutputDir, projectDirectory);
            if (!Directory.Exists(subProjectDirectory))
            {
                throw new DirectoryNotFoundException($"Directory {subProjectDirectory} was not found.");
            }

            var subProject = new Project
            {
                Output = project.Output,
                DiagnosticsMessageSink = project.DiagnosticsMessageSink,
                ProjectName = projectName,
                TemplateOutputDir = subProjectDirectory,
            };

            return subProject;
        }

        public static bool TryValidateBrowserRequired(BrowserKind browserKind, bool isRequired, out string error)
        {
            error = !isRequired ? null : $"Browser '{browserKind}' is required but not configured on '{RuntimeInformation.OSDescription}'";
            return isRequired;
        }

        protected void EnsureBrowserAvailable(BrowserKind browserKind)
        {
            Assert.False(
                TryValidateBrowserRequired(
                    browserKind,
                    isRequired: !BrowserManager.IsExplicitlyDisabled(browserKind),
                    out var errorMessage),
                errorMessage);
        }
    }
}
