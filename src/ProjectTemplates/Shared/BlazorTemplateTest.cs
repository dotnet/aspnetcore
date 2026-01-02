// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Blazor.Test;

public abstract class BlazorTemplateTest : LoggedTest
{
    public BlazorTemplateTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; set; }

    private ITestOutputHelper _output;
    public ITestOutputHelper Output
    {
        get
        {
            if (_output == null)
            {
                _output = new TestOutputLogger(Logger);
            }
            return _output;
        }
    }

    public abstract string ProjectType { get; }

    protected async Task<Project> CreateBuildPublishAsync(string auth = null, string[] args = null, string targetFramework = null, bool serverProject = false, bool onlyCreate = false)
    {
        // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
        Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

        var project = await ProjectFactory.CreateProject(Output);
        if (targetFramework != null)
        {
            project.TargetFramework = targetFramework;
        }

        await project.RunDotNetNewAsync(ProjectType, auth: auth, args: args, errorOnRestoreError: false);

        if (serverProject || auth is null)
        {
            // External auth mechanisms (which is any auth at all for Blazor WASM) require https to work and thus don't honor the --no-https flag
            var requiresHttps = string.Equals(auth, "IndividualB2C", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(auth, "SingleOrg", StringComparison.OrdinalIgnoreCase)
                                || (!serverProject && auth is not null);
            var noHttps = args?.Contains(ArgConstants.NoHttps) ?? false;
            var expectedLaunchProfileNames = requiresHttps
                ? new[] { "https" }
                : noHttps
                    ? new[] { "http" }
                    : new[] { "http", "https" };
            await project.VerifyLaunchSettings(expectedLaunchProfileNames);
        }

        if (!onlyCreate)
        {
            var targetProject = project;
            if (serverProject)
            {
                targetProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");
            }

            await targetProject.RunDotNetPublishAsync(noRestore: false);
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

    protected static string ReadFile(string basePath, string path)
    {
        var fullPath = Path.Combine(basePath, path);
        var doesExist = File.Exists(fullPath);

        Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
        return File.ReadAllText(Path.Combine(basePath, path));
    }
}
