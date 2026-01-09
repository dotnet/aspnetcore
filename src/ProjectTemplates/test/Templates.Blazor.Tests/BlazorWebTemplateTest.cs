// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.InternalTesting;
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

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task BlazorWebTemplateLocalhostTldWithDots()
    {
        var project = await ProjectFactory.CreateProject(Output, "my.namespace.blazor");

        await project.RunDotNetNewAsync("blazor", args: new[] { ArgConstants.LocalhostTld, ArgConstants.NoInteractivity });

        var expectedLaunchProfileNames = new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);
        await VerifyDnsCompliantHostname(project, "my-namespace-blazor");
    }

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task BlazorWebTemplateLocalhostTldWithLeadingDot()
    {
        var project = await ProjectFactory.CreateProject(Output, ".StartWithDot");

        await project.RunDotNetNewAsync("blazor", args: new[] { ArgConstants.LocalhostTld, ArgConstants.NoInteractivity });

        var expectedLaunchProfileNames = new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);
        await VerifyDnsCompliantHostname(project, "startwithdot");
    }

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task BlazorWebTemplateLocalhostTldWithTrailingDot()
    {
        var project = await ProjectFactory.CreateProject(Output, "EndWithDot.");

        await project.RunDotNetNewAsync("blazor", args: new[] { ArgConstants.LocalhostTld, ArgConstants.NoInteractivity });

        var expectedLaunchProfileNames = new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);
        await VerifyDnsCompliantHostname(project, "endwithdot");
    }

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task BlazorWebTemplateLocalhostTldWithMultipleInvalidChars()
    {
        var project = await ProjectFactory.CreateProject(Output, "My..Test__Project");

        await project.RunDotNetNewAsync("blazor", args: new[] { ArgConstants.LocalhostTld, ArgConstants.NoInteractivity });

        var expectedLaunchProfileNames = new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);
        await VerifyDnsCompliantHostname(project, "my--test--project");
    }

    private async Task VerifyDnsCompliantHostname(Project project, string expectedHostname)
    {
        var launchSettingsPath = Path.Combine(project.TemplateOutputDir, "Properties", "launchSettings.json");
        Assert.True(File.Exists(launchSettingsPath), $"launchSettings.json not found at {launchSettingsPath}");

        var launchSettingsContent = await File.ReadAllTextAsync(launchSettingsPath);
        using var launchSettings = JsonDocument.Parse(launchSettingsContent);

        var profiles = launchSettings.RootElement.GetProperty("profiles");

        foreach (var profile in profiles.EnumerateObject())
        {
            if (profile.Value.TryGetProperty("applicationUrl", out var applicationUrl))
            {
                var urls = applicationUrl.GetString();
                if (!string.IsNullOrEmpty(urls))
                {
                    // Verify the hostname in the URL matches expected DNS-compliant format
                    Assert.Contains($"{expectedHostname}.dev.localhost:", urls);
                    
                    // Verify no underscores in hostname (RFC 952/1123 compliance)
                    var hostnamePattern = @"://([^:]+)\.dev\.localhost:";
                    var matches = System.Text.RegularExpressions.Regex.Matches(urls, hostnamePattern);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var hostname = match.Groups[1].Value;
                        Assert.DoesNotContain("_", hostname);
                        Assert.DoesNotContain(".", hostname);
                        Assert.False(hostname.StartsWith("-", StringComparison.Ordinal), $"Hostname '{hostname}' should not start with hyphen (RFC 952/1123 violation)");
                        Assert.False(hostname.EndsWith("-", StringComparison.Ordinal), $"Hostname '{hostname}' should not end with hyphen (RFC 952/1123 violation)");
                    }
                }
            }
        }
    }
}
