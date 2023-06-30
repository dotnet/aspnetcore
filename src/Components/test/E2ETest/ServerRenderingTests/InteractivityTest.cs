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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_Server(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, serverIncrement: 3));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-server"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);

        var countServerElem = Browser.FindElement(By.Id("count-server"));
        Browser.Equal("0", () => countServerElem.Text);

        Browser.Click(By.Id("increment-server"));
        Browser.Equal("3", () => countServerElem.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_WebAssembly(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, webAssemblyIncrement: 4));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-wasm"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);

        var countWasmElem = Browser.FindElement(By.Id("count-wasm"));
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-wasm"));
        Browser.Equal("4", () => countWasmElem.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_ServerAndWebAssembly(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, serverIncrement: 10, webAssemblyIncrement: 11));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-server"));
            Browser.DoesNotExist(By.Id("count-wasm"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);

        var countServerElem = Browser.FindElement(By.Id("count-server"));
        var countWasmElem = Browser.FindElement(By.Id("count-wasm"));
        Browser.Equal("0", () => countServerElem.Text);
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-server"));
        Browser.Equal("10", () => countServerElem.Text);
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-wasm"));
        Browser.Equal("11", () => countWasmElem.Text);
        Browser.Equal("10", () => countServerElem.Text);
    }

    private string InteractiveCallsiteUrl(bool prerender, int? serverIncrement = default, int? webAssemblyIncrement = default)
    {
        var result = $"{ServerPathBase}/interactive-callsite?suppress-autostart&prerender={prerender}";

        if (serverIncrement.HasValue)
        {
            result += $"&server={serverIncrement}";
        }

        if (webAssemblyIncrement.HasValue)
        {
            result += $"&wasm={webAssemblyIncrement}";
        }

        return result;
    }
}
