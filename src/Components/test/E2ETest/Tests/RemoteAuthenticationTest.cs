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

    [Fact]
    public void NavigateToLogin_PreservesExtraQueryParams()
    {
        // If the preservedExtraQueryParams passed to NavigateToLogin by RedirectToLogin gets trimmed,
        // the OIDC endpoints will fail to authenticate the user.
        Navigate("/subdir/test-remote-authentication");

        var heading = Browser.Exists(By.TagName("h1"));
        Browser.Equal("Hello, Jane Doe!", () => heading.Text);
    }

    [Fact]
    public void NavigateToLogin_MapsCallbackErrorCodeToSafeMessage()
    {
        // The client uses the authorization code flow, so the callback parameters are in the
        // query string. The provider supplied error_description must not reach the UI.
        NavigateWithFreshAuthenticationState("/subdir/test-remote-authentication?callbackResponseMode=query&callbackError=access_denied&callbackErrorDescription=sensitive-provider-message");

        var messageText = "";
        Browser.Equal(
            "There was an error trying to log you in: 'Access was denied during sign-in.'",
            () => messageText = Browser.FindElement(By.TagName("p")).Text);
        Assert.DoesNotContain("sensitive-provider-message", messageText);
    }

    [Fact]
    public void NavigateToLogin_IgnoresCallbackParametersOutsideTheConfiguredResponseMode()
    {
        // The callback is well formed in the query string, but an unrelated 'access_denied' is
        // also present in the fragment. The client is configured for the authorization code
        // flow, whose parameters are defined to be in the query, so the fragment must be
        // ignored. Reading the fragment first would surface the stray error instead.
        NavigateWithFreshAuthenticationState("/subdir/test-remote-authentication?callbackResponseMode=strayFragment&callbackError=access_denied");

        var messageText = "";
        Browser.Equal(
            "There was an error trying to log you in: 'There was an error signing in.'",
            () => messageText = Browser.FindElement(By.TagName("p")).Text);
        Assert.DoesNotContain("Access was denied", messageText);
    }

    // Every test in this class shares one browser session, and oidc-client caches the
    // signed-in user in sessionStorage, which survives same-tab navigation. A test that
    // runs after a successful login would load the app already authenticated and never
    // reach RedirectToLogin. Storage is origin-scoped, so clear it from a same-origin
    // endpoint that neither boots the app nor starts an authentication flow.
    private void NavigateWithFreshAuthenticationState(string url)
    {
        Navigate("/subdir/oidc/.well-known/openid-configuration");
        ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.clear(); localStorage.clear();");
        Navigate(url);
    }
}
