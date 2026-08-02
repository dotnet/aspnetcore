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

public class ServerRenderedAuthenticationStateTest
     : ServerTestBase<TrimmingServerFixture<RazorComponentEndpointsStartup<App>>>
{
    public ServerRenderedAuthenticationStateTest(
        BrowserFixture browserFixture,
        TrimmingServerFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        // Test with AuthenticationStateSerializationOptions.SerializeAllClaims = true since that keeps the Server and WebAssembly
        // behavior as similar as possible. The default behavior is tested by DefaultAuthenticationStateSerializationOptionsTest.
        serverFixture.AdditionalArguments.Add("SerializeAllClaims=true");
    }

    [Theory]
    [InlineData("Static")]
    [InlineData("Server")]
    [InlineData("WebAssembly")]
    public void CanUseServerAuthenticationState(string platform)
    {
        var pageName = $"{platform}{(platform != "Static" ? "-interactive" : "")}-authentication-state";
        Navigate($"{ServerPathBase}/auth/{pageName}");

        VerifyLoggedOut(platform);

        Browser.Click(By.LinkText("Log in"));

        VerifyLoggedIn(platform);

        Browser.Click(By.LinkText("Log out"));

        VerifyLoggedOut(platform);
    }

    [Fact]
    public void CanUseCustomNameAndRoleTypeOnWebAssembly()
    {
        Navigate($"{ServerPathBase}/auth/webassembly-interactive-authentication-state?roleClaimType=role&nameClaimType=name");

        VerifyLoggedOut("WebAssembly");
        
        Browser.Click(By.LinkText("Log in"));

        VerifyLoggedIn("WebAssembly");
        Browser.Equal("(none)", () => Browser.FindElement(By.Id("additional-claim")).Text);

        Browser.Click(By.LinkText("Log out"));

        VerifyLoggedOut("WebAssembly");
    }

    [Fact]
    public void CanCustomizeAuthenticationStateDeserialization()
    {
        Navigate($"{ServerPathBase}/auth/webassembly-interactive-authentication-state?additionalClaim=Custom%20claim%20value");

        VerifyLoggedOut("WebAssembly");
        Browser.Equal("(none)", () => Browser.FindElement(By.Id("additional-claim")).Text);

        Browser.Click(By.LinkText("Log in"));

        VerifyLoggedIn("WebAssembly");
        Browser.Equal("Custom claim value", () => Browser.FindElement(By.Id("additional-claim")).Text);

        Browser.Click(By.LinkText("Log out"));

        VerifyLoggedOut("WebAssembly");
        Browser.Equal("(none)", () => Browser.FindElement(By.Id("additional-claim")).Text);
    }

    private void VerifyPlatform(string platform)
    {
        Browser.Equal((platform != "Static").ToString(), () => Browser.FindElement(By.Id("is-interactive")).Text);
        Browser.Equal(platform, () => Browser.FindElement(By.Id("platform")).Text);
    }

    private void VerifyLoggedOut(string platform)
    {
        VerifyPlatform(platform);
        Browser.Equal("False", () => Browser.FindElement(By.Id("identity-authenticated")).Text);
        Browser.Equal("", () => Browser.FindElement(By.Id("identity-name")).Text);
        Browser.Equal("(none)", () => Browser.FindElement(By.Id("test-claim")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-in-test-role-1")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-in-test-role-2")).Text);
    }

    private void VerifyLoggedIn(string platform)
    {
        VerifyPlatform(platform);
        Browser.Equal("True", () => Browser.FindElement(By.Id("identity-authenticated")).Text);
        Browser.Equal("YourUsername", () => Browser.FindElement(By.Id("identity-name")).Text);
        Browser.Equal("Test claim value", () => Browser.FindElement(By.Id("test-claim")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-in-test-role-1")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-in-test-role-2")).Text);
    }
}
