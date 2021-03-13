// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ProjectTemplates.Tests.Infrastructure;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public abstract class BlazorTemplateTest
    {
        public const int BUILDCREATEPUBLISH_PRIORITY = -1000;

        public BlazorTemplateTest(ProjectFactoryFixture projectFactory, PlaywrightFixture<BlazorServerTemplateTest> browserFixture, ITestOutputHelper output)
        {
            Fixture = browserFixture;
            ProjectFactory = projectFactory;
            Output = output;
            BrowserContextInfo = new ContextInformation(CreateFactory(output));
        }

        public PlaywrightFixture<BlazorServerTemplateTest> Fixture { get; }
        public ProjectFactoryFixture ProjectFactory { get; set; }
        public ITestOutputHelper Output { get; }
        public Project Project { get; protected set; }
        public ContextInformation BrowserContextInfo { get; }
        public abstract string ProjectType { get; }


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

        protected async Task CreateBuildPublishAsync(string projectName, string auth = null, string[] args = null, string targetFramework = null, bool serverProject = false)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            Project = await ProjectFactory.GetOrCreateProject(projectName, Output);
            if (targetFramework != null)
            {
                Project.TargetFramework = targetFramework;
            }

            var createResult = await Project.RunDotNetNewAsync(ProjectType, auth: auth, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var targetProject = Project;
            if (serverProject)
            {
                targetProject = GetSubProject(Project, "Server", $"{Project.ProjectName}.Server");
            }

            var publishResult = await targetProject.RunDotNetPublishAsync(noRestore: !serverProject);
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", targetProject, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await targetProject.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", targetProject, buildResult));
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
                    isRequired: !Fixture.BrowserManager.IsExplicitlyDisabled(browserKind),
                    out var errorMessage),
                errorMessage);
        }
    }
}
