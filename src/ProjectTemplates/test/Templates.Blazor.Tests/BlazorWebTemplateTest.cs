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

    [Theory]
    [InlineData(BrowserKind.Chromium, "WebAssembly")]
    [InlineData(BrowserKind.Chromium, "Auto")]
    public async Task BlazorWebWebWorkerTemplate_Works(BrowserKind browserKind, string interactivityOption)
    {
        var project = await CreateBuildPublishAsync(
            args: ["-int", interactivityOption, "--webworker"],
            getTargetProject: GetTargetProject);

        var appName = project.ProjectName;

        // Verify the WorkerClient project was created
        var workerClientDir = Path.Combine(project.TemplateOutputDir, "..", $"{appName}.WorkerClient");
        Assert.True(Directory.Exists(workerClientDir), "WebWorker templates should produce a WorkerClient project");

        await TestProjectCoreAsync(project, browserKind, BlazorTemplatePages.None, AuthenticationFeatures.None);

        // Test the ImageProcessor page loads
        await TestImageProcessorPageAsync(project, browserKind);

        Project GetTargetProject(Project rootProject)
        {
            // Multiple projects were created, so we need to specifically select the server
            // project to be used
            return GetSubProject(rootProject, rootProject.ProjectName, rootProject.ProjectName);
        }
    }

    private async Task TestImageProcessorPageAsync(Project project, BrowserKind browserKind)
    {
        using var aspNetProcess = project.StartBuiltProjectAsync();

        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project for ImageProcessor test", project, aspNetProcess.Process));

        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
        var page = await browser.NewPageAsync();

        var imageProcessorUrl = $"{aspNetProcess.ListeningUri.AbsoluteUri.TrimEnd('/')}/imageprocessor";
        Output.WriteLine($"Opening browser at {imageProcessorUrl}...");
        await page.GotoAsync(imageProcessorUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Verify the page title and content
        await page.WaitForSelectorAsync("h1 >> text=Image Processor");

        // Verify the Worker Status element exists
        await page.WaitForSelectorAsync("text=Worker Status:");

        // Verify the file input exists
        await page.WaitForSelectorAsync("input[type='file']");

        await page.CloseAsync();
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
