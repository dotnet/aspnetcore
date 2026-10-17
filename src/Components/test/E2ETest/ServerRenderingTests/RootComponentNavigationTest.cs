// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class RootComponentNavigationTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public RootComponentNavigationTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public async Task NavigationDuringCircuitHandlerInitializationAppliesTheNextRootComponent()
    {
        var token = Guid.NewGuid().ToString("N");
        using var http = new HttpClient { BaseAddress = _serverFixture.RootUri };

        using var armResponse = await http.PostAsync($"{ServerPathBase}/navigation-gate/arm/{token}", content: null);
        armResponse.EnsureSuccessStatusCode();
        try
        {
            Navigate($"{ServerPathBase}/navigation-gate/first");
            Browser.Exists(By.Id("navigation-gate-next"));
            var entered = false;
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            while (!entered && DateTime.UtcNow < deadline)
            {
                using var response = await http.GetAsync($"{ServerPathBase}/navigation-gate/entered/{token}");
                entered = response.IsSuccessStatusCode;
                if (!entered)
                {
                    await Task.Delay(50);
                }
            }

            Assert.True(entered, "The initial circuit handler did not enter the gate.");

            Browser.Click(By.Id("navigation-gate-next"));
            Browser.Exists(By.Id("navigation-gate-second-button"));
        }
        finally
        {
            using var releaseResponse = await http.PostAsync($"{ServerPathBase}/navigation-gate/release/{token}", content: null);
            releaseResponse.EnsureSuccessStatusCode();
        }

        Browser.Equal("True", () => Browser.FindElement(By.Id("navigation-gate-interactive")).Text);
        Browser.Click(By.Id("navigation-gate-second-button"));
        Browser.Equal("Second clicks: 1", () => Browser.FindElement(By.Id("navigation-gate-second-button")).Text);
    }
}
