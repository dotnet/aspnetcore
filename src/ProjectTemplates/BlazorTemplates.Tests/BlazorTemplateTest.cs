// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Templates.Test.Helpers;
using Xunit;

namespace Templates.Test
{
    public abstract class BlazorTemplateTest : BrowserTestBase
    {
        public const int BUILDCREATEPUBLISH_PRIORITY = -1000;

        public BlazorTemplateTest(ProjectFactoryFixture projectFactory)
        {
            ProjectFactory = projectFactory;
        }

        public ProjectFactoryFixture ProjectFactory { get; set; }
        
        public abstract string ProjectType { get; }

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

                var publishResult = await targetProject.RunDotNetPublishAsync(noRestore: false);
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
