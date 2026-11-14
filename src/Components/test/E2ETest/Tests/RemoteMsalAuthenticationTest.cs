// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class RemoteMsalAuthenticationTest :
    ServerTestBase<TrimmingServerFixture<RemoteMsalAuthenticationStartup>>
{
    public RemoteMsalAuthenticationTest(
        BrowserFixture browserFixture,
        TrimmingServerFixture<RemoteMsalAuthenticationStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void NavigateDirectlyToLoginCallbackRestartsLoginWithoutInteropFailure()
    {
        Navigate("/subdir/msal-authentication/login-callback");

        Browser.True(() => (bool?)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return window.__msalSignInCalled") == true);

        var browserLog = Browser.Manage().Logs.GetLog(LogType.Browser);
        Assert.DoesNotContain(browserLog, entry => entry.Message.Contains(
            "The value 'AuthenticationService.isTopLevelWindow' is not a function.",
            StringComparison.Ordinal));
    }
}
