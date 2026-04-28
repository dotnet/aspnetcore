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

public class RenderFragmentSerializationTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public RenderFragmentSerializationTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void RenderFragmentCrossesBoundary_SimpleText(string mode)
    {
        Navigate($"{ServerPathBase}/render-fragment-interactive?test=1&mode={mode}");

        Browser.Equal("Hello from SSR", () => Browser.FindElement(By.Id("simple-text")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-test1")).Text);

        Browser.Click(By.Id("increment-test1"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-test1")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void RenderFragmentCrossesBoundary_NestedElements(string mode)
    {
        Navigate($"{ServerPathBase}/render-fragment-interactive?test=2&mode={mode}");

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-test2")).Text);

        Browser.Contains("Title", () => Browser.FindElement(By.Id("nested-elements")).FindElement(By.TagName("h2")).Text);
        Browser.Equal(2, () => Browser.FindElement(By.Id("nested-elements")).FindElements(By.TagName("li")).Count);
        Browser.Equal("Item 1", () => Browser.FindElement(By.Id("nested-elements")).FindElements(By.TagName("li"))[0].Text);
        Browser.Equal("Item 2", () => Browser.FindElement(By.Id("nested-elements")).FindElements(By.TagName("li"))[1].Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void RenderFragmentCrossesBoundary_MixedContent(string mode)
    {
        Navigate($"{ServerPathBase}/render-fragment-interactive?test=3&mode={mode}");

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-test3")).Text);

        Browser.Contains("bold", () => Browser.FindElement(By.Id("mixed-content")).FindElement(By.TagName("strong")).Text);
        Browser.Contains("italic", () => Browser.FindElement(By.Id("mixed-content")).FindElement(By.TagName("em")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void RenderFragmentCrossesBoundary_WithAttributes(string mode)
    {
        Navigate($"{ServerPathBase}/render-fragment-interactive?test=4&mode={mode}");

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-test4")).Text);

        Browser.Equal("Attributed content", () => Browser.FindElement(By.Id("with-attributes")).Text);
        Browser.Equal("styled", () => Browser.FindElement(By.Id("with-attributes")).GetAttribute("class"));
        Browser.Equal("42", () => Browser.FindElement(By.Id("with-attributes")).GetAttribute("data-value"));
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public void RenderFragmentCrossesBoundary_NestedRenderFragment(string mode)
    {
        Navigate($"{ServerPathBase}/render-fragment-interactive?test=5&mode={mode}");

        Browser.Equal("Nested RenderFragment content", () => Browser.FindElement(By.Id("nested-child")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-test5")).Text);

        Browser.Click(By.Id("increment-test5"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-test5")).Text);

        Browser.Click(By.Id("increment-test5-inner"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-test5-inner")).Text);
    }
}
