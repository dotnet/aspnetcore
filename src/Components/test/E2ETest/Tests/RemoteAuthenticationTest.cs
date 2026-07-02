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
    public void NavigateToLogin_HandlesCallbackErrorsFromUrlFragments()
    {
        Navigate("/subdir/test-remote-authentication?callbackResponseMode=fragment&callbackError=access_denied&callbackErrorDescription=sensitive-provider-message");

        var message = Browser.Exists(By.TagName("p"));
        Browser.Equal("There was an error trying to log you in: 'Access was denied during sign-in.'", () => message.Text);
        Assert.DoesNotContain("sensitive-provider-message", message.Text);
    }
}
