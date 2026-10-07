// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.BrowserTesting;
using Templates.Test.Helpers;

namespace BlazorTemplates.Tests;

public class BlazorWebTemplateTest(ProjectFactoryFixture projectFactory) : BlazorTemplateTest(projectFactory), IClassFixture<ProjectFactoryFixture>
{
    public override string ProjectType => "blazor";

    [Theory]
    [InlineData(BrowserKind.Chromium, "None")]
    [InlineData(BrowserKind.Chromium, "Server")]
    [InlineData(BrowserKind.Chromium, "WebAssembly")]
    [InlineData(BrowserKind.Chromium, "Auto")]
    [InlineData(BrowserKind.Chromium, "None", "Individual")]
    [InlineData(BrowserKind.Chromium, "Server", "Individual")]
    [InlineData(BrowserKind.Chromium, "WebAssembly", "Individual")]
    [InlineData(BrowserKind.Chromium, "Auto", "Individual")]
    [InlineData(BrowserKind.Chromium, "Server", "None", true)]
    [InlineData(BrowserKind.Chromium, "WebAssembly", "None", true)]
    [InlineData(BrowserKind.Chromium, "Auto", "None", true)]
    [InlineData(BrowserKind.Chromium, "Server", "Individual", true)]
    [InlineData(BrowserKind.Chromium, "WebAssembly", "Individual", true)]
    [InlineData(BrowserKind.Chromium, "Auto", "Individual", true)]
    public async Task BlazorWebTemplate_Works(BrowserKind browserKind, string interactivityOption, string authOption = "None", bool allInteractive = false)
    {
        var project = await CreateBuildPublishAsync(
            args: ["-int", interactivityOption, "-au", authOption, "-ai", allInteractive ? "true" : "false"],
            getTargetProject: GetTargetProject);

        var routesDirectory = HasClientProject() && allInteractive
            ? Path.Combine(project.TemplateOutputDir, "..", $"{project.ProjectName}.Client")
            : Path.Combine(project.TemplateOutputDir, "Components");
        var routes = await File.ReadAllTextAsync(Path.Combine(routesDirectory, "Routes.razor"));
        Assert.Contains("DefaultLayout=\"typeof(MainLayout)\"", routes);
        Assert.DoesNotContain("DefaultLayout=\"typeof(Layout.MainLayout)\"", routes);

        var imports = await File.ReadAllTextAsync(Path.Combine(routesDirectory, "_Imports.razor"));
        var layoutNamespace = HasClientProject() && allInteractive
            ? $"{project.ProjectName}.Client.Layout"
            : $"{project.ProjectName}.Components.Layout";
        Assert.Contains($"@using {layoutNamespace}", imports);

        // There won't be a counter page when the 'None' interactivity option is used
        var pagesToExclude = interactivityOption is "None"
            ? BlazorTemplatePages.Counter
            : BlazorTemplatePages.None;

        var authenticationFeatures = authOption is "None"
            ? AuthenticationFeatures.None
            : AuthenticationFeatures.RegisterAndLogIn;

        await TestProjectCoreAsync(project, browserKind, pagesToExclude, authenticationFeatures);

        bool HasClientProject()
            => interactivityOption is "WebAssembly" or "Auto";

        Project GetTargetProject(Project rootProject)
        {
            if (HasClientProject())
            {
                // Multiple projects were created, so we need to specifically select the server
                // project to be used
                return GetSubProject(rootProject, rootProject.ProjectName, rootProject.ProjectName);
            }

            // In other cases, just use the root project
            return rootProject;
        }
    }

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWebTemplate_CanUsePasskeys(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync(args: ["-int", "None", "-au", "Individual"]);
        var pagesToExclude = BlazorTemplatePages.Counter;
        var authenticationFeatures = AuthenticationFeatures.RegisterAndLogIn | AuthenticationFeatures.Passkeys;

        await TestProjectCoreAsync(project, browserKind, pagesToExclude, authenticationFeatures);
    }

    private async Task TestProjectCoreAsync(Project project, BrowserKind browserKind, BlazorTemplatePages pagesToExclude, AuthenticationFeatures authenticationFeatures)
    {
        var appName = project.ProjectName;

        // Test the built project
        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            await TestBasicInteractionInNewPageAsync(browserKind, aspNetProcess.ListeningUri.AbsoluteUri, appName, pagesToExclude, authenticationFeatures);
        }

        // Test the published project
        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            await TestBasicInteractionInNewPageAsync(browserKind, aspNetProcess.ListeningUri.AbsoluteUri, appName, pagesToExclude, authenticationFeatures);
        }
    }
}
