// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class RemoteAuthenticationTest :
    ServerTestBase<TrimmingServerFixture<RemoteAuthenticationStartup>>
{
    public RemoteAuthenticationTest(
        BrowserFixture browserFixture,
        TrimmingServerFixture<RemoteAuthenticationStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => base.InitializeAsync(Guid.NewGuid().ToString());

    [Fact]
    public void ConcurrentMsalInitialization_UsesSingleInitializedClient()
    {
        Navigate("/subdir/msal-initialization.html");

        var result = Browser.Exists(By.Id("result"));
        Browser.Equal("PASS", () => result.GetDomAttribute("data-status"));

        Browser.Navigate().Refresh();
        result = Browser.Exists(By.Id("result"));
        Browser.Equal("PASS", () => result.GetDomAttribute("data-status"));
    }

    [Fact]
    public void NavigateToLogin_PreservesExtraQueryParams()
    {
        // If the preservedExtraQueryParams passed to NavigateToLogin by RedirectToLogin gets trimmed,
        // the OIDC endpoint will not observe it.
        Navigate("/subdir/test-remote-authentication");

        var heading = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello, Jane Doe!", () => heading.Text);
        Browser.Equal("true", () => GetResponseText("/subdir/oidc/preserved-extra-query-params-observed"));
    }

    [Fact]
    public void NavigateDirectlyToLoginCallbackRestartsLogin()
    {
        Navigate("/subdir/authentication/login-callback");

        Browser.Equal(new Uri(_serverFixture.RootUri, "/subdir/").AbsoluteUri, () => Browser.Url);
        var heading = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello, Jane Doe!", () => heading.Text);
    }

    [Fact]
    public void SilentLoginCallbackInIframeDoesNotRestartLogin()
    {
        Navigate("/subdir/oidc/mount-silent-callback");

        Browser.Exists(By.XPath("//p[text()='Silent authentication completed.']"));
        Browser.True(() => (bool?)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return window.__authenticationCallbackCompletedInIframe") == true);
        Browser.DoesNotExist(By.TagName("iframe"));
        Browser.Equal("0", () => GetResponseText("/subdir/oidc/interactive-authorization-requests"));
    }

    private string GetResponseText(string path)
    {
        var response = ((IJavaScriptExecutor)Browser).ExecuteAsyncScript(
            """
            const done = arguments[arguments.length - 1];
            fetch(arguments[0]).then(response => response.text()).then(done);
            """,
            path);

        return Assert.IsType<string>(response);
    }
}
