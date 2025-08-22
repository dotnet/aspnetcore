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
    public async Task BlazorWebTemplate_Works(BrowserKind browserKind, string interactivityOption, string authOption = "None")
    {
        var project = await CreateBuildPublishAsync(
            args: ["-int", interactivityOption, "-au", authOption],
            getTargetProject: GetTargetProject);

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
