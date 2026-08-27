// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text.Json;
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
        Environment.SetEnvironmentVariable("AllowMissingPrunePackageData", "true");

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
        AuthenticationFeatures authenticationFeatures = AuthenticationFeatures.None)
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

        await TestBasicInteractionAsync(browser, page, appName, pagesToExclude, authenticationFeatures);

        await page.CloseAsync();
    }

    protected async Task TestBasicInteractionAsync(
        IBrowserContext browser,
        IPage page,
        string appName,
        BlazorTemplatePages pagesToExclude = BlazorTemplatePages.None,
        AuthenticationFeatures authenticationFeatures = AuthenticationFeatures.None)
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

        if (authenticationFeatures.HasFlag(AuthenticationFeatures.RegisterAndLogIn))
        {
            // Start a new CDP session with WebAuthn enabled and add a virtual authenticator.
            // We do this regardless of whether we're testing passkeys, because passkey
            // gets attempted unconditionally on the login page, and this utilizes the WebAuthn API.
            await using var cdpSession = await browser.NewCDPSessionAsync(page);
            await cdpSession.SendAsync("WebAuthn.enable");
            var result = await cdpSession.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
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

            Assert.True(result.HasValue);
            var authenticatorId = result.Value.GetProperty("authenticatorId").GetString();
            Assert.NotNull(authenticatorId);

            // Record the WebAuthn signal calls made by each page so that we can assert on them later.
            // We define the signal methods if they're missing so that the assertions don't depend on
            // the browser version bundled with Playwright.
            await page.AddInitScriptAsync("""
                window.__passkeySignals = [];
                window.__passkeyAutofillStarted = false;
                window.__resolveUnknownCredentialSignal = null;
                if (navigator.credentials) {
                    const originalGet = navigator.credentials.get.bind(navigator.credentials);
                    navigator.credentials.get = async function (options) {
                        const credential = await originalGet(options);
                        sessionStorage.setItem('__passkeyCredentialJson', JSON.stringify(credential));
                        return credential;
                    };
                }
                if (window.PublicKeyCredential) {
                    if (sessionStorage.getItem('__forcePasskeyAutofillOnce')) {
                        sessionStorage.removeItem('__forcePasskeyAutofillOnce');
                        window.PublicKeyCredential.isConditionalMediationAvailable = () => {
                            window.__passkeyAutofillStarted = true;
                            return new Promise(() => {});
                        };
                    } else if (sessionStorage.getItem('__skipPasskeyAutofillOnce')) {
                        sessionStorage.removeItem('__skipPasskeyAutofillOnce');
                        window.PublicKeyCredential.isConditionalMediationAvailable = () => Promise.resolve(false);
                    }
                    for (const name of ['signalAllAcceptedCredentials', 'signalCurrentUserDetails', 'signalUnknownCredential']) {
                        window.PublicKeyCredential[name] = function (options) {
                            window.__passkeySignals.push({ name, options });
                            return name === 'signalUnknownCredential'
                                ? new Promise(resolve => window.__resolveUnknownCredentialSignal = resolve)
                                : Promise.resolve();
                        };
                    }
                }
                """);

            await Task.WhenAll(
                page.WaitForURLAsync("**/Account/Login**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Login"));

            await Task.WhenAll(
                page.WaitForURLAsync("**/Account/Register**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Register as a new user"));

            await page.WaitForSelectorAsync("text=Create a new account.");

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = "[PLACEHOLDER]-1a";

            await page.FillAsync("[name=\"Input.Email\"]", userName);
            await page.FillAsync("[name=\"Input.Password\"]", password);
            await page.FillAsync("[name=\"Input.ConfirmPassword\"]", password);

            // We will be redirected to the RegisterConfirmation
            await Task.WhenAll(
                page.WaitForURLAsync("**/Account/RegisterConfirmation**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("button[type=\"submit\"]"));

            // We will be redirected to the ConfirmEmail
            await Task.WhenAll(
                page.WaitForURLAsync("**/Account/ConfirmEmail**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Click here to confirm your account"));

            // Now we attempt to navigate to the "Auth Required" page,
            // which should redirect us to the login page since we are not logged in
            await Task.WhenAll(
                page.WaitForURLAsync("**/Account/Login**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Auth Required"));

            // Now we can login
            await page.WaitForSelectorAsync("[name=\"Input.Email\"]");
            await page.FillAsync("[name=\"Input.Email\"]", userName);
            await page.FillAsync("[name=\"Input.Password\"]", password);
            await page.ClickAsync("button[type=\"submit\"]");

            // Verify that we return to the "Auth Required" page
            await page.WaitForSelectorAsync("text=You are authenticated");

            if (authenticationFeatures.HasFlag(AuthenticationFeatures.Passkeys))
            {
                // Navigate to the passkey management page
                await ClearPasskeySignalsAsync(page);
                await Task.WhenAll(
                    page.WaitForURLAsync("**/Account/Manage**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                    page.ClickAsync("a[href=\"Account/Manage\"]"));

                await page.WaitForSelectorAsync("text=Manage your account");

                // The profile page signals the browser's passkey provider with the user's current details
                var userDetails = await GetPasskeySignalAsync(page, "signalCurrentUserDetails");
                Assert.Equal(new Uri(page.Url).Host, userDetails.GetProperty("rpId").GetString());
                Assert.Equal(userName, userDetails.GetProperty("name").GetString());
                Assert.Equal(userName, userDetails.GetProperty("displayName").GetString());
                await AssertSignalRetriesAfterFailureAsync(
                    page,
                    "current-user-details-signal",
                    "signalCurrentUserDetails");

                // Check that an error is displayed if passkey creation fails
                await Task.WhenAll(
                    page.WaitForURLAsync("**/Account/Manage/Passkeys**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                    page.ClickAsync("a[href=\"Account/Manage/Passkeys\"]"));
                await AssertSignalRetriesAfterFailureAsync(
                    page,
                    "all-accepted-credentials-signal",
                    "signalAllAcceptedCredentials");

                // Adding a passkey requires a confirmation first, so the add button is not shown yet
                await page.WaitForSelectorAsync("text=Confirm it's you");
                Assert.Equal(0, await page.Locator("text=Add a new passkey").CountAsync());

                // The add form is rejected until the confirmation is done
                await page.EvaluateAsync("""
                    () => {
                        const form = document.createElement('form');
                        form.method = 'post';
                        form.action = location.pathname;
                        const fields = {
                            '_handler': 'add-passkey',
                            'Input.CredentialJson': '{}',
                        };
                        const token = document.querySelector('input[name="__RequestVerificationToken"]');
                        if (token) {
                            fields['__RequestVerificationToken'] = token.value;
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
                    """);

                await page.WaitForSelectorAsync("text=Error: You must confirm your identity before adding a passkey.");
                await page.WaitForSelectorAsync("text=No passkeys are registered.");

                // Confirm with the account password to unlock the add button
                await page.FillAsync("[name=\"Input.Password\"]", password);
                await page.ClickAsync("text=Confirm password");
                await page.WaitForSelectorAsync("text=Add a new passkey");

                await page.EvaluateAsync("""
                    () => {
                        navigator.credentials.create = () => {
                            const error = new Error("Simulated passkey creation failure");
                            error.name = "NotAllowedError";
                            return Promise.reject(error);
                        };
                    }
                    """);

                await page.ClickAsync("text=Add a new passkey");
                await page.WaitForSelectorAsync("text=Error: No passkey was provided by the authenticator.");

                // Now check that we can successfully register a passkey
                await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
                await page.ClickAsync("text=Add a new passkey");

                await page.WaitForSelectorAsync("text=Enter a name for your passkey");

                // First check that we can't register a passkey with a long name.
                var longName = new string('a', count: 201);
                await page.FillAsync("[name=\"Input.Name\"]", longName);
                await page.ClickAsync("text=Continue");
                await page.WaitForSelectorAsync("text=Passkey names must be no longer than 200 characters.");

                // Now register a passkey with a valid name
                await page.FillAsync("[name=\"Input.Name\"]", "My passkey");
                await ClearPasskeySignalsAsync(page);
                await page.ClickAsync("text=Continue");

                await page.WaitForSelectorAsync("text=Passkey updated successfully");

                // The page signals the browser's passkey provider with the passkeys that are
                // still valid, so that deleted ones stop being offered at sign-in.
                var acceptedCredentials = await GetSignalledCredentialIdsAsync(page);
                var storedCredentials = await GetAuthenticatorCredentialsAsync(cdpSession, authenticatorId);
                Assert.Single(storedCredentials);
                Assert.Equal(storedCredentials, acceptedCredentials);
                var passkeyCredentialId = storedCredentials[0];

                // Logout so that we can test the passkey login flow
                await Task.WhenAll(
                    page.WaitForURLAsync("**/Account/Login**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                    page.ClickAsync("text=Logout"));

                // Navigate home to reset the return URL
                await page.ClickAsync("text=Home");
                await page.WaitForSelectorAsync("text=Hello, world!");

                // Now navigate to the login page
                await Task.WhenAll(
                    page.WaitForURLAsync("**/Account/Login**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                    page.ClickAsync("text=Login"));

                // Check that an error is displayed if passkey retrieval fails
                await page.EvaluateAsync("""
                    () => {
                        navigator.credentials.get = () => {
                            const error = new Error("Simulated passkey retrieval failure");
                            error.name = "NotAllowedError";
                            return Promise.reject(error);
                        };
                    }
                    """);

                await page.ClickAsync("text=Log in with a passkey");
                await page.WaitForSelectorAsync("text=Error: No passkey was provided by the authenticator.");

                // Now check that we can successfully login with the passkey
                await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
                await page.WaitForSelectorAsync("[name=\"Input.Email\"]");
                await page.FillAsync("[name=\"Input.Email\"]", userName);
                await page.ClickAsync("text=Log in with a passkey");

                // Verify that we return to the home page
                await page.WaitForSelectorAsync("text=Hello, world!");

                // Verify that we can visit the "Auth Required" page again
                await page.ClickAsync("text=Auth Required");
                await page.WaitForSelectorAsync("text=You are authenticated");

                // Deleting the passkey signals the provider with an empty credential list,
                // which is what removes the passkey from the sign-in options
                await Task.WhenAll(
                    page.WaitForURLAsync("**/Account/Manage**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                    page.ClickAsync("a[href=\"Account/Manage\"]"));

                await Task.WhenAll(
                    page.WaitForURLAsync("**/Account/Manage/Passkeys**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                    page.ClickAsync("a[href=\"Account/Manage/Passkeys\"]"));

                await ClearPasskeySignalsAsync(page);
                await page.ClickAsync("button[value=\"delete\"]");
                await page.WaitForSelectorAsync("text=Passkey deleted successfully");

                Assert.Empty(await GetSignalledCredentialIdsAsync(page));

                // Submit the revoked credential again. The unknown credential signal remains pending,
                // so a conditional autofill request can only start if the template gets the ordering wrong.
                await page.EvaluateAsync("() => sessionStorage.setItem('__skipPasskeyAutofillOnce', 'true')");
                await Task.WhenAll(
                    page.WaitForURLAsync("**/Account/Login**", new() { WaitUntil = WaitUntilState.NetworkIdle }),
                    page.ClickAsync("text=Logout"));

                await page.EvaluateAsync("""
                    () => {
                        const credentialJson = sessionStorage.getItem('__passkeyCredentialJson');
                        if (!credentialJson) {
                            throw new Error('The revoked passkey credential was not captured.');
                        }
                        sessionStorage.setItem('__forcePasskeyAutofillOnce', 'true');
                        navigator.credentials.get = () => Promise.resolve(JSON.parse(credentialJson));
                    }
                    """);

                await page.FillAsync("[name=\"Input.Email\"]", userName);
                await ClearPasskeySignalsAsync(page);
                await page.ClickAsync("text=Log in with a passkey");
                await page.WaitForSelectorAsync("text=Error: Invalid login attempt.");
                var unknownCredential = await GetPasskeySignalAsync(page, "signalUnknownCredential");
                Assert.Equal(new Uri(page.Url).Host, unknownCredential.GetProperty("rpId").GetString());
                Assert.Equal(passkeyCredentialId, unknownCredential.GetProperty("credentialId").GetString());
                Assert.False(await page.EvaluateAsync<bool>("() => window.__passkeyAutofillStarted"));

                await page.EvaluateAsync("() => window.__resolveUnknownCredentialSignal()");
                await page.WaitForFunctionAsync("() => window.__passkeyAutofillStarted");
            }
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

        static Task ClearPasskeySignalsAsync(IPage page)
        {
            // Discards signals recorded so far so that GetPasskeySignalAsync can only see the ones
            // that follow. AddInitScriptAsync only resets the array on a new document, and enhanced
            // navigation does not start one.
            return page.EvaluateAsync("() => { window.__passkeySignals = []; }");
        }

        static async Task<JsonElement> GetPasskeySignalAsync(IPage page, string name)
        {
            await page.WaitForFunctionAsync($"() => window.__passkeySignals.some(s => s.name === '{name}')");
            // Read the most recent signal, since a single navigation can record more than one.
            return await page.EvaluateAsync<JsonElement>($"() => window.__passkeySignals.findLast(s => s.name === '{name}').options");
        }

        static async Task<string[]> GetSignalledCredentialIdsAsync(IPage page)
        {
            var options = await GetPasskeySignalAsync(page, "signalAllAcceptedCredentials");
            return [.. options.GetProperty("allAcceptedCredentialIds").EnumerateArray().Select(id =>
            {
                var credentialId = id.GetString();
                Assert.NotNull(credentialId);
                return credentialId;
            })];
        }

        static async Task AssertSignalRetriesAfterFailureAsync(IPage page, string selector, string method)
        {
            await page.WaitForSelectorAsync(selector, new() { State = WaitForSelectorState.Attached });
            var attempts = await page.EvaluateAsync<int>(
                """
                async ({ selector, method }) => {
                    const element = document.querySelector(selector);
                    const originalSignal = window.PublicKeyCredential[method];
                    let attempts = 0;
                    window.PublicKeyCredential[method] = function (options) {
                        attempts++;
                        return attempts === 1
                            ? Promise.reject(new Error('Simulated signal failure'))
                            : originalSignal.call(window.PublicKeyCredential, options);
                    };

                    try {
                        const options = ` ${element.getAttribute('options')}`;
                        element.setAttribute('options', options);
                        await new Promise(resolve => setTimeout(resolve));
                        element.removeAttribute('options');
                        element.setAttribute('options', options);

                        for (let i = 0; i < 10 && attempts < 2; i++) {
                            await new Promise(resolve => setTimeout(resolve, 10));
                        }

                        return attempts;
                    } finally {
                        window.PublicKeyCredential[method] = originalSignal;
                    }
                }
                """,
                new { selector, method });

            Assert.Equal(2, attempts);
        }

        static async Task<string[]> GetAuthenticatorCredentialsAsync(ICDPSession cdpSession, string authenticatorId)
        {
            var result = await cdpSession.SendAsync("WebAuthn.getCredentials", new Dictionary<string, object>
            {
                ["authenticatorId"] = authenticatorId,
            });
            var credentials = result.Value.GetProperty("credentials").EnumerateArray();
            // The signal API uses base64url while CDP uses base64.
            return [.. credentials.Select(c =>
            {
                var credentialId = c.GetProperty("credentialId").GetString();
                Assert.NotNull(credentialId);
                return Base64Url.EncodeToString(Convert.FromBase64String(credentialId));
            })];
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

    [Flags]
    protected enum AuthenticationFeatures
    {
        None = 0,
        RegisterAndLogIn = 1,
        Passkeys = 2,
    }
}
