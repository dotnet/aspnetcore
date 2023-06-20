// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class InteractivityTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public InteractivityTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CanRenderInteractiveServerComponent()
    {
        // '2' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?server=2");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);

        Browser.Click(By.Id("increment-server"));

        Browser.Equal("2", () => Browser.FindElement(By.Id("count-server")).Text);
    }

    [Fact]
    public void CanRenderInteractiveServerComponentFromRazorClassLibrary()
    {
        // '3' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?server-shared=3");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server-shared")).Text);

        Browser.Click(By.Id("increment-server-shared"));

        Browser.Equal("3", () => Browser.FindElement(By.Id("count-server-shared")).Text);
    }

    [Fact]
    public void CanRenderInteractiveWebAssemblyComponentFromRazorClassLibrary()
    {
        // '4' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?wasm-shared=4");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm-shared")).Text);

        Browser.Click(By.Id("increment-wasm-shared"));

        Browser.Equal("4", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
    }

    [Fact]
    public void CanRenderInteractiveServerAndWebAssemblyComponentsAtTheSameTime()
    {
        // '3' and '5' configure the increment amounts.
        Navigate($"{ServerPathBase}/interactive?server-shared=3&wasm-shared=5");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm-shared")).Text);

        Browser.Click(By.Id("increment-server-shared"));
        Browser.Click(By.Id("increment-wasm-shared"));

        Browser.Equal("3", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("5", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
    }
}
