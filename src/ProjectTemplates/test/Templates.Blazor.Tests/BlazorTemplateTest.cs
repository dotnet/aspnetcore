// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.Playwright;
using Templates.Test.Helpers;

namespace BlazorTemplates.Tests;

public abstract class BlazorTemplateTest : BrowserTestBase
{
    public const int BUILDCREATEPUBLISH_PRIORITY = -1000;

    public BlazorTemplateTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
        Microsoft.Playwright.Program.Main(["install"]);
    }

    public ProjectFactoryFixture ProjectFactory { get; set; }

    public abstract string ProjectType { get; }

    protected async Task<Project> CreateBuildPublishAsync(
        string auth = null,
        string[] args = null,
        string targetFramework = null,
        Func<Project, Project> getTargetProject = null,
        bool onlyCreate = false)
    {
        // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
        Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

        var project = await ProjectFactory.CreateProject(Output);
        if (targetFramework != null)
        {
            project.TargetFramework = targetFramework;
        }

        await project.RunDotNetNewAsync(ProjectType, auth: auth, args: args);

        project = getTargetProject?.Invoke(project) ?? project;

        if (!onlyCreate)
        {
            await project.RunDotNetPublishAsync(noRestore: false);

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            await project.RunDotNetBuildAsync();
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

    protected async Task TestBasicInteractionInNewPageAsync(
        BrowserKind browserKind,
        string listeningUri,
        string appName,
        BlazorTemplatePages pagesToExclude = BlazorTemplatePages.None,
        bool usesAuth = false)
    {
        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
        var page = await browser.NewPageAsync();

        Output.WriteLine($"Opening browser at {listeningUri}...");
        await page.GotoAsync(listeningUri, new() { WaitUntil = WaitUntilState.NetworkIdle });

        await TestBasicInteractionAsync(page, appName, pagesToExclude, usesAuth);

        await page.CloseAsync();
    }

    protected async Task TestBasicInteractionAsync(
        IPage page,
        string appName,
        BlazorTemplatePages pagesToExclude = BlazorTemplatePages.None,
        bool usesAuth = false)
    {
        await page.WaitForSelectorAsync("nav");

        if (!pagesToExclude.HasFlag(BlazorTemplatePages.Home))
        {
            // Initially displays the home page
            await page.WaitForSelectorAsync("h1 >> text=Hello, world!");

            Assert.Equal("Home", (await page.TitleAsync()).Trim());
        }

        if (!pagesToExclude.HasFlag(BlazorTemplatePages.Counter))
        {
            // Can navigate to the counter page
            await Task.WhenAll(
                page.WaitForURLAsync("**/counter"),
                page.WaitForSelectorAsync("h1 >> text=Counter"),
                page.WaitForSelectorAsync("p >> text=Current count: 0"),
                page.ClickAsync("a[href=counter]"));

            // Clicking the counter button works
            await IncrementCounterAsync(page);
        }

        if (usesAuth)
        {
            await Task.WhenAll(
                page.WaitForURLAsync("**/Identity/Account/Login**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Log in"));

            await Task.WhenAll(
                page.WaitForSelectorAsync("[name=\"Input.Email\"]"),
                page.WaitForURLAsync("**/Identity/Account/Register**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Register as a new user"));

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = "[PLACEHOLDER]-1a";

            await page.FillAsync("[name=\"Input.Email\"]", userName);
            await page.FillAsync("[name=\"Input.Password\"]", password);
            await page.FillAsync("[name=\"Input.ConfirmPassword\"]", password);

            // We will be redirected to the RegisterConfirmation
            await Task.WhenAll(
                page.WaitForURLAsync("**/Identity/Account/RegisterConfirmation**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("#registerSubmit"));

            // We will be redirected to the ConfirmEmail
            await Task.WhenAll(
                page.WaitForURLAsync("**/Identity/Account/ConfirmEmail**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Click here to confirm your account"));

            // Now we can login
            await page.ClickAsync("text=Login");
            await page.WaitForSelectorAsync("[name=\"Input.Email\"]");
            await page.FillAsync("[name=\"Input.Email\"]", userName);
            await page.FillAsync("[name=\"Input.Password\"]", password);
            await page.ClickAsync("#login-submit");

            // Need to navigate to fetch page
            await page.GotoAsync(new Uri(page.Url).GetLeftPart(UriPartial.Authority));
            Assert.Equal(appName.Trim(), (await page.TitleAsync()).Trim());
        }

        if (!pagesToExclude.HasFlag(BlazorTemplatePages.Weather))
        {
            await page.ClickAsync("a[href=weather]");
            await page.WaitForSelectorAsync("h1 >> text=Weather");

            // Asynchronously loads and displays the table of weather forecasts
            await page.WaitForSelectorAsync("table>tbody>tr");
            Assert.Equal(5, await page.Locator("p+table>tbody>tr").CountAsync());
        }

        static async Task IncrementCounterAsync(IPage page)
        {
            // Allow multiple click attempts because some interactive render modes
            // won't be immediately available
            const int MaxIncrementAttempts = 5;
            const float IncrementTimeoutMilliseconds = 3000f;
            for (var i = 0; i < MaxIncrementAttempts; i++)
            {
                await page.ClickAsync("p+button >> text=Click me");
                try
                {
                    await page.WaitForSelectorAsync("p >> text=Current count: 1", new()
                    {
                        Timeout = IncrementTimeoutMilliseconds,
                    });

                    // The counter successfully incremented, so we're done
                    return;
                }
                catch (TimeoutException)
                {
                    // The counter did not increment; try again
                }
            }

            Assert.Fail($"The counter did not increment after {MaxIncrementAttempts} attempts");
        }
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

    [Flags]
    protected enum BlazorTemplatePages
    {
        None = 0,
        Home = 1,
        Counter = 2,
        Weather = 4,
        All = ~0,
    }
}
