// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.AuthTests;

public class DefaultAuthenticationStateSerializationOptionsTest
     : ServerTestBase<TrimmingServerFixture<RazorComponentEndpointsStartup<App>>>
{
    public DefaultAuthenticationStateSerializationOptionsTest(
        BrowserFixture browserFixture,
        TrimmingServerFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void DoesNotSerializeAllClaimsByDefault()
    {
        Navigate($"{ServerPathBase}/auth/webassembly-interactive-authentication-state?roleClaimType=role&nameClaimType=name");

        Browser.Click(By.LinkText("Log in"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("platform")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("identity-authenticated")).Text);
        Browser.Equal("YourUsername", () => Browser.FindElement(By.Id("identity-name")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-in-test-role-1")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-in-test-role-2")).Text);

        // While the name and role claims are serialized by default, the test claim is not.
        Browser.Equal("(none)", () => Browser.FindElement(By.Id("test-claim")).Text);
    }
}
