// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerScriptReconnectionTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public ServerScriptReconnectionTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Theory]
    [InlineData(false, "web")]
    [InlineData(true, "server")]
    public void ReconnectsToExistingCircuit(bool useBlazorServerJs, string script)
    {
        var path = $"{ServerPathBase}/persistent-state/disconnection";
        Navigate(useBlazorServerJs ? $"{path}?use-blazor-server-js=true" : path);

        var javascript = (IJavaScriptExecutor)Browser;
        var loadedScripts = javascript.ExecuteScript("""
            return performance.getEntriesByType('resource')
                .filter(resource => resource.responseStatus === 200)
                .map(resource => new URL(resource.name).pathname.match(/\/_framework\/blazor\.(server|web)(?:\.[^./]+)?\.js$/)?.[1])
                .filter(Boolean)
                .join(',');
            """);
        Assert.Equal(script, loadedScripts);

        var counter = By.Id("non-persisted-counter");
        var increment = By.Id("increment-non-persisted-counter");
        Browser.Equal("True", () => Browser.Exists(By.Id("render-mode-interactive")).Text);
        Browser.Exists(increment).Click();
        Browser.Equal("6", () => Browser.Exists(counter).Text);
        Browser.True(HasConnectedWebSocket);

        ForceCloseConnection(javascript);
        Browser.True(HasConnectedWebSocket);

        Browser.Equal("6", () => Browser.Exists(counter).Text);
        Browser.Exists(increment).Click();
        Browser.Equal("7", () => Browser.Exists(counter).Text);
    }

    private bool HasConnectedWebSocket() =>
        Browser.Manage().Logs.GetLog(LogType.Browser)
            .Any(entry => entry.Level == LogLevel.Info && entry.Message.Contains("Information: WebSocket connected"));

    private static void ForceCloseConnection(IJavaScriptExecutor javascript)
    {
        var error = javascript.ExecuteAsyncScript("""
            const done = arguments[arguments.length - 1];
            // Import the URL that startup used so the browser returns the same module instance.
            const initializer = performance.getEntriesByType('resource').find(resource =>
                /\/Components\.TestServer(?:\.[^./]+)?\.lib\.module\.js$/.test(new URL(resource.name).pathname));
            if (!initializer) {
                done('The Components.TestServer initializer was not loaded.');
                return;
            }

            import(initializer.name)
                .then(module => module.forceCloseConnection())
                .then(() => done(null), error => done(String(error)));
            """);
        Assert.Null(error);
    }
}
