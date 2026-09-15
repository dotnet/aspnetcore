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
    [InlineData(BrowserKind.Chromium, "Server", "None", true)]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/66403")]
    public async Task BlazorWebTemplate_Works(BrowserKind browserKind, string interactivityOption, string authOption = "None", bool allInteractive = false)
    {
        string[] args = allInteractive
            ? ["-int", interactivityOption, "-au", authOption, ArgConstants.GlobalInteractivity]
            : ["-int", interactivityOption, "-au", authOption];

        var project = await CreateBuildPublishAsync(
            args: args,
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
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/69095")]
    public async Task BlazorWebTemplate_CanUsePasskeys(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync(args: ["-int", "None", "-au", "Individual"]);
        var pagesToExclude = BlazorTemplatePages.Counter;
        var authenticationFeatures = AuthenticationFeatures.RegisterAndLogIn | AuthenticationFeatures.Passkeys;

        await TestProjectCoreAsync(project, browserKind, pagesToExclude, authenticationFeatures);
    }

    [Theory]
    [InlineData(BrowserKind.Chromium, "Success", "Success")]
    [InlineData(BrowserKind.Chromium, "Failure", "Success")]
    [InlineData(BrowserKind.Chromium, "SavedThenFailure", "Success")]
    [InlineData(BrowserKind.Chromium, "Failure", "Rejected")]
    [InlineData(BrowserKind.Chromium, "Failure", "Pending")]
    [InlineData(BrowserKind.Chromium, "Failure", "Unsupported")]
    [InlineData(BrowserKind.Chromium, "DatabaseFailure", "Success")]
    [InlineData(BrowserKind.Chromium, "LookupFailure", "Success")]
    public async Task BlazorWebTemplate_ConditionalPasskeyRegistration(
        BrowserKind browserKind, string persistenceOutcome, string signalOutcome)
    {
        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        var project = await CreateBuildPublishAsync(args: ["-int", "None", "-au", "Individual"], onlyCreate: true);
        AddPasskeyUpgradeTestEndpoints(project);
        await project.RunDotNetBuildAsync();

        using var aspNetProcess = project.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));
        await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

        await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
        await browser.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["X-Passkey-Test-Outcome"] = persistenceOutcome,
        });
        var page = await browser.NewPageAsync();
        await using var cdpSession = await browser.NewCDPSessionAsync(page);
        await cdpSession.SendAsync("WebAuthn.enable");
        var authenticator = await cdpSession.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new
            {
                protocol = "ctap2",
                transport = "internal",
                hasResidentKey = true,
                hasUserVerification = true,
                isUserVerified = true,
                automaticPresenceSimulation = true,
            }
        });
        Assert.True(authenticator.HasValue);
        var authenticatorId = authenticator.Value.GetProperty("authenticatorId").GetString();
        Assert.NotNull(authenticatorId);
        await cdpSession.SendAsync("WebAuthn.setResponseOverrideBits", new Dictionary<string, object>
        {
            ["authenticatorId"] = authenticatorId,
            ["isBadUP"] = true,
            ["isBadUV"] = true,
        });

        await page.AddInitScriptAsync($$"""
            PublicKeyCredential.isConditionalMediationAvailable = async () => false;
            PublicKeyCredential.getClientCapabilities = async () => ({ conditionalCreate: true });

            const originalCreate = navigator.credentials.create.bind(navigator.credentials);
            navigator.credentials.create = async options => {
                const calls = Number(sessionStorage.getItem('upgrade-create-calls') ?? 0) + 1;
                sessionStorage.setItem('upgrade-create-calls', calls);
                if (options.mediation !== 'conditional') {
                    throw new Error('Expected conditional passkey creation.');
                }

                // The virtual authenticator cannot satisfy password-manager eligibility for conditional creation.
                const credential = await originalCreate({ ...options, mediation: 'optional' });
                sessionStorage.setItem('upgrade-credential', JSON.stringify({
                    userId: new TextDecoder().decode(options.publicKey.user.id),
                    userName: options.publicKey.user.name,
                    credentialId: credential.id,
                    flags: new Uint8Array(credential.response.getAuthenticatorData())[32],
                }));
                return credential;
            };

            PublicKeyCredential.signalUnknownCredential = '{{signalOutcome}}' === 'Unsupported'
                ? undefined
                : options => {
                    const signals = JSON.parse(sessionStorage.getItem('upgrade-signals') ?? '[]');
                    signals.push(options);
                    sessionStorage.setItem('upgrade-signals', JSON.stringify(signals));
                    if ('{{signalOutcome}}' === 'Rejected') {
                        return Promise.reject(new Error('Simulated signal rejection.'));
                    }
                    if ('{{signalOutcome}}' === 'Pending') {
                        return new Promise(() => {});
                    }
                    return Promise.resolve();
                };
            """);

        var listeningUri = aspNetProcess.ListeningUri.AbsoluteUri;
        await page.GotoAsync($"{listeningUri}Account/Register", new() { WaitUntil = WaitUntilState.NetworkIdle });
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

        var returnUrl = new Uri(new Uri(page.Url), "/auth?from=conditional-passkey&value=one%20two").AbsoluteUri;
        await page.GotoAsync(returnUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForURLAsync("**/Account/Login**", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.FillAsync("[name=\"Input.Email\"]", userName);
        await page.FillAsync("[name=\"Input.Password\"]", password);

        var upgradeRequests = new List<IRequest>();
        page.Request += (_, request) =>
        {
            if (request.Method == "POST" && new Uri(request.Url).AbsolutePath == "/Account/PasskeyUpgrade")
            {
                upgradeRequests.Add(request);
            }
        };
        var upgradeResponseTask = page.WaitForResponseAsync(response =>
            response.Request.Method == "POST" && new Uri(response.Url).AbsolutePath == "/Account/PasskeyUpgrade");
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in", Exact = true }).ClickAsync();
        var upgradeResponse = await upgradeResponseTask;
        await page.WaitForURLAsync(returnUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 });
        await page.WaitForSelectorAsync("text=You are authenticated");
        Assert.Equal(returnUrl, page.Url);

        var credential = await page.EvaluateAsync<JsonElement>("JSON.parse(sessionStorage.getItem('upgrade-credential'))");
        var account = await page.EvaluateAsync<JsonElement>("""
            async () => {
                const response = await fetch('/test/passkeys');
                if (!response.ok) {
                    throw new Error(`Passkey inspection failed: ${response.status}`);
                }
                return await response.json();
            }
            """);
        var state = account.GetProperty("state");
        Assert.Equal(1, await page.EvaluateAsync<int>("Number(sessionStorage.getItem('upgrade-create-calls'))"));
        Assert.Equal(userName, account.GetProperty("email").GetString());
        Assert.Equal(userName, credential.GetProperty("userName").GetString());
        Assert.Equal(account.GetProperty("id").GetString(), credential.GetProperty("userId").GetString());
        Assert.Equal(account.GetProperty("id").GetString(), state.GetProperty("userId").GetString());
        Assert.Equal(credential.GetProperty("credentialId").GetString(), state.GetProperty("credentialId").GetString());
        Assert.Equal(0x40, credential.GetProperty("flags").GetInt32());
        Assert.False(state.GetProperty("isUserVerified").GetBoolean());
        Assert.Equal(1, state.GetProperty("saveAttempts").GetInt32());
        Assert.Equal(persistenceOutcome, state.GetProperty("outcome").GetString());

        var shouldPersist = persistenceOutcome is "Success" or "SavedThenFailure";
        Assert.Equal(shouldPersist, state.GetProperty("saved").GetBoolean());
        var passkeys = account.GetProperty("passkeys").EnumerateArray().ToArray();
        if (shouldPersist)
        {
            var passkey = Assert.Single(passkeys);
            Assert.Equal(credential.GetProperty("credentialId").GetString(), passkey.GetProperty("credentialId").GetString());
            Assert.False(passkey.GetProperty("isUserVerified").GetBoolean());
        }
        else
        {
            Assert.Empty(passkeys);
        }

        var shouldSignal = persistenceOutcome is "Failure" or "DatabaseFailure";
        var signals = await page.EvaluateAsync<JsonElement>("JSON.parse(sessionStorage.getItem('upgrade-signals') ?? '[]')");
        if (shouldSignal && signalOutcome is not "Unsupported")
        {
            var signal = Assert.Single(signals.EnumerateArray());
            Assert.Equal(new Uri(listeningUri).Host, signal.GetProperty("rpId").GetString());
            Assert.Equal(credential.GetProperty("credentialId").GetString(), signal.GetProperty("credentialId").GetString());
        }
        else
        {
            Assert.Empty(signals.EnumerateArray());
        }

        Assert.Equal(shouldSignal ? 2 : 1, upgradeRequests.Count);
        Assert.Contains("_handler=passkey-upgrade", upgradeRequests[0].PostData);
        var registrationHeaders = await upgradeRequests[0].AllHeadersAsync();
        Assert.Contains("Identity.TwoFactorUserId=", registrationHeaders["cookie"]);
        Assert.Contains("Input.CredentialJson=", upgradeRequests[0].PostData);
        if (shouldSignal)
        {
            Assert.Equal(200, upgradeResponse.Status);
            var redisplayedPage = await upgradeResponse.TextAsync();
            Assert.Contains("unknown-credential-signal-options=", redisplayedPage);
            Assert.DoesNotContain("creation-options=", redisplayedPage);
            Assert.Contains("_handler=passkey-upgrade", upgradeRequests[1].PostData);
            var continuationHeaders = await upgradeRequests[1].AllHeadersAsync();
            Assert.DoesNotContain("Identity.TwoFactorUserId=", continuationHeaders["cookie"]);
            Assert.DoesNotContain("Input.CredentialJson=", upgradeRequests[1].PostData);
            Assert.DoesNotContain("Input.Error=", upgradeRequests[1].PostData);
        }
        if (persistenceOutcome is "LookupFailure")
        {
            Assert.True(state.GetProperty("lookupFailed").GetBoolean());
        }
    }

    private static void AddPasskeyUpgradeTestEndpoints(Project project)
    {
        var testAsset = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestAssets", "PasskeyUpgradeTest.cs"));
        File.WriteAllText(
            Path.Combine(project.TemplateOutputDir, "PasskeyUpgradeTest.cs"),
            testAsset.Replace("TestApp", project.ProjectName, StringComparison.Ordinal));

        var programPath = Path.Combine(project.TemplateOutputDir, "Program.cs");
        var program = File.ReadAllText(programPath);
        const string buildMarker = "var app = builder.Build();";
        const string runMarker = "app.Run();";
        Assert.Contains(buildMarker, program);
        Assert.Contains(runMarker, program);
        program = program.Replace(
            buildMarker,
            $"{project.ProjectName}.PasskeyUpgradeTest.AddServices(builder.Services);{Environment.NewLine}{Environment.NewLine}{buildMarker}",
            StringComparison.Ordinal);
        program = program.Replace(
            runMarker,
            $"{project.ProjectName}.PasskeyUpgradeTest.MapEndpoints(app);{Environment.NewLine}{Environment.NewLine}{runMarker}",
            StringComparison.Ordinal);
        File.WriteAllText(programPath, program);
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
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/69095")]
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
