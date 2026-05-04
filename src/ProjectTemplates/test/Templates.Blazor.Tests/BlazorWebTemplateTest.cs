// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Playwright;
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
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/66403")]
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

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWebTemplate_ErrorPage_RendersCorrectly(BrowserKind browserKind)
    {
        // Test that the Error page renders correctly with Server interactivity.
        // This validates that the [PersistentState] attribute on the public RequestId property works,
        // which previously failed with a private property because PersistentState requires the getter to be public.
        var project = await CreateBuildPublishAsync(args: ["-int", "Server"]);

        using var aspNetProcess = project.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
        await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
        var page = await browser.NewPageAsync();
        await page.GotoAsync(new Uri(aspNetProcess.ListeningUri, "/Error").AbsoluteUri, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("h1.text-danger >> text=Error.");
        await page.WaitForSelectorAsync("h2.text-danger >> text=An error occurred while processing your request.");
        // Verify the Request ID is shown, confirming the public [PersistentState] RequestId property is populated.
        // A private property would cause PersistentValueProviderComponentSubscription to throw during rendering.
        await page.WaitForSelectorAsync("strong >> text=Request ID:");
        await page.CloseAsync();
    }

    [ConditionalTheory]
    [InlineData("my.namespace.blazor", "my-namespace-blazor")]
    [InlineData(".StartWithDot", "startwithdot")]
    [InlineData("EndWithDot.", "endwithdot")]
    [InlineData("My..Test__Project", "my-test-project")]
    [InlineData("Project123.Test456", "project123-test456")]
    [InlineData("xn--My.Test.Project", "xn-my-test-project")]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task BlazorWebTemplateLocalhostTld_GeneratesDnsCompliantHostnames(string projectName, string expectedHostname)
    {
        var project = await ProjectFactory.CreateProject(Output, projectName);

        await project.RunDotNetNewAsync("blazor", args: new[] { ArgConstants.LocalhostTld, ArgConstants.NoInteractivity });

        var expectedLaunchProfileNames = new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);
        await project.VerifyDnsCompliantHostname(expectedHostname);
    }
}
