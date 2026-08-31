// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
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

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWebTemplate_RequiresReauthenticationForNewCredentials(BrowserKind browserKind)
    {
        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        var project = await CreateBuildPublishAsync(args: ["-int", "None", "-au", "Individual"], onlyCreate: true);
        AddRemovePasswordTestEndpoint(project);
        await project.RunDotNetBuildAsync();

        using var aspNetProcess = project.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));
        await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

        await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
        var page = await browser.NewPageAsync();
        await using var cdpSession = await browser.NewCDPSessionAsync(page);
        await cdpSession.SendAsync("WebAuthn.enable");
        await cdpSession.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new
            {
                protocol = "ctap2",
                transport = "internal",
                hasResidentKey = false,
                hasUserVerification = true,
                isUserVerified = true,
                automaticPresenceSimulation = true,
            }
        });
        await page.AddInitScriptAsync("""
            if (window.PublicKeyCredential) {
                window.PublicKeyCredential.isConditionalMediationAvailable = () => Promise.resolve(false);
            }
            """);

        var listeningUri = aspNetProcess.ListeningUri.AbsoluteUri;
        await page.GotoAsync(listeningUri, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Task.WhenAll(
            page.WaitForURLAsync("**/Account/Login**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
            page.ClickAsync("text=Login"));
        await Task.WhenAll(
            page.WaitForURLAsync("**/Account/Register**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
            page.ClickAsync("text=Register as a new user"));

        var userName = $"{Guid.NewGuid()}@example.com";
        var password = "[PLACEHOLDER]-1a";
        await Task.WhenAll(
            page.WaitForURLAsync("**/Account/RegisterConfirmation**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
            SubmitFormAsync(page, "register", new Dictionary<string, string>
            {
                ["Input.Email"] = userName,
                ["Input.Password"] = password,
                ["Input.ConfirmPassword"] = password,
            }));
        await Task.WhenAll(
            page.WaitForURLAsync("**/Account/ConfirmEmail**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
            page.ClickAsync("text=Click here to confirm your account"));

        await page.GotoAsync($"{listeningUri}Account/Login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Task.WhenAll(
            page.WaitForSelectorAsync("h1 >> text=Hello, world!"),
            SubmitFormAsync(page, "login", new Dictionary<string, string>
            {
                ["Input.Email"] = userName,
                ["Input.Password"] = password,
            }));

        await page.GotoAsync($"{listeningUri}Account/Manage/Passkeys", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("text=Confirm it's you");
        Assert.Equal(400, await GetPasskeyCreationOptionsStatusAsync(page));

        await page.FillAsync("[name=\"Input.Password\"]", password);
        await page.ClickAsync("text=Confirm password");
        await page.WaitForSelectorAsync("text=Add a new passkey");
        Assert.Equal(200, await GetPasskeyCreationOptionsStatusAsync(page));

        await page.ClickAsync("text=Add a new passkey");
        await page.WaitForSelectorAsync("text=Enter a name for your passkey");
        await page.FillAsync("[name=\"Input.Name\"]", "My passkey");
        await page.ClickAsync("text=Continue");
        await page.WaitForSelectorAsync("text=Passkey updated successfully");

        var removePasswordStatus = await page.EvaluateAsync<int>(
            "async () => (await fetch('/test/remove-password', { method: 'POST' })).status");
        Assert.Equal(200, removePasswordStatus);

        await page.GotoAsync($"{listeningUri}Account/Manage/SetPassword", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("text=Confirm it's you");
        Assert.Equal(0, await page.Locator("button:has-text(\"Set password\")").CountAsync());

        var newPassword = "[PLACEHOLDER]-2b";
        await page.EvaluateAsync(
            """
            password => {
                const handler = document.querySelector('input[name="_handler"][value="set-password"]');
                if (!handler) {
                    throw new Error('The set-password form was not found.');
                }

                const form = handler.closest('form');
                for (const [name, value] of Object.entries({
                    'Input.NewPassword': password,
                    'Input.ConfirmPassword': password,
                })) {
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = name;
                    input.value = value;
                    form.appendChild(input);
                }
                form.submit();
            }
            """,
            newPassword);

        await page.WaitForSelectorAsync("text=Error: You must confirm your identity before setting a password.");
        await page.ClickAsync("text=Confirm with a passkey");
        await page.WaitForSelectorAsync("button:has-text(\"Set password\")");
        await page.FillAsync("[name=\"Input.NewPassword\"]", newPassword);
        await page.FillAsync("[name=\"Input.ConfirmPassword\"]", newPassword);
        await Task.WhenAll(
            page.WaitForURLAsync("**/Account/Manage/ChangePassword", new() { WaitUntil = WaitUntilState.NetworkIdle }),
            page.ClickAsync("button:has-text(\"Set password\")"));
        await page.WaitForSelectorAsync("button:has-text(\"Update password\")");
    }

    private static void AddRemovePasswordTestEndpoint(Project project)
    {
        var programPath = Path.Combine(project.TemplateOutputDir, "Program.cs");
        var program = File.ReadAllText(programPath);
        var updatedProgram = program.Replace(
            "app.Run();",
            """
            app.MapPost("/test/remove-password", async (HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                return user is not null && (await userManager.RemovePasswordAsync(user)).Succeeded
                    ? Results.Ok()
                    : Results.BadRequest();
            }).RequireAuthorization();

            app.Run();
            """,
            StringComparison.Ordinal);

        Assert.NotEqual(program, updatedProgram);
        File.WriteAllText(programPath, updatedProgram);
    }

    private static Task<int> GetPasskeyCreationOptionsStatusAsync(IPage page)
        => page.EvaluateAsync<int>(
            """
            async () => {
                return (await fetch('/Account/Manage/PasskeyCreationOptions', {
                    method: 'POST',
                })).status;
            }
            """);

    private static Task SubmitFormAsync(IPage page, string handler, Dictionary<string, string> fields)
        => page.EvaluateAsync(
            """
            ({ handler, fields }) => {
                const form = document.createElement('form');
                form.method = 'post';
                form.action = location.pathname;
                fields._handler = handler;
                const token = document.querySelector('input[name="__RequestVerificationToken"]');
                if (token) {
                    fields.__RequestVerificationToken = token.value;
                }

                for (const [name, value] of Object.entries(fields)) {
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = name;
                    input.value = value;
                    form.appendChild(input);
                }

                document.body.appendChild(form);
                form.submit();
            }
            """,
            new { handler, fields });

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWebTemplate_CanRequireConfirmedEmail(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync(
            args: ["-int", "None", "-au", "Individual"],
            onlyCreate: true);

        var programPath = Path.Combine(project.TemplateOutputDir, "Program.cs");
        var program = await File.ReadAllTextAsync(programPath);
        const string requireConfirmedAccount = "options.SignIn.RequireConfirmedAccount = true;";
        Assert.Contains(requireConfirmedAccount, program);
        program = program.Replace(
            requireConfirmedAccount,
            "options.SignIn.RequireConfirmedEmail = true;",
            StringComparison.Ordinal);
        await File.WriteAllTextAsync(programPath, program);

        await project.RunDotNetPublishAsync(noRestore: false);
        await project.RunDotNetBuildAsync();

        await TestProjectCoreAsync(
            project,
            browserKind,
            BlazorTemplatePages.Counter,
            AuthenticationFeatures.RegisterAndLogIn);
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
