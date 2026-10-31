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

public class UnifiedRoutingTests : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public UnifiedRoutingTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Theory]
    [InlineData("routing/parameters/value", "value")]
    // Issue 53138
    [InlineData("%F0%9F%99%82/routing/parameters/http%3A%2F%2Fwww.example.com%2Flogin%2Fcallback", "http://www.example.com/login/callback")]
    // Note this double encodes the final 2 slashes
    [InlineData("%F0%9F%99%82/routing/parameters/http%3A%2F%2Fwww.example.com%2520login%2520callback", "http://www.example.com%20login%20callback")]
    // Issue 53262
    [InlineData("routing/parameters/%21%40%23%24%25%5E%26%2A%28%29_%2B-%3D%5B%5D%7B%7D%5C%5C%7C%3B%27%3A%5C%22%3E%3F.%2F", """!@#$%^&*()_+-=[]{}\\|;':\">?./""")]
    // Issue 52808
    [InlineData("routing/parameters/parts%20w%2F%20issue", "parts w/ issue")]
    public void Routing_CanRenderPagesWithParameters_And_TransitionToInteractive(string url, string expectedValue)
    {
        ExecuteRoutingTestCore(url, expectedValue);
    }

    [Theory]
    [InlineData("routing/constraints/5", "5")]
    [InlineData("%F0%9F%99%82/routing/constraints/http%3A%2F%2Fwww.example.com%2Flogin%2Fcallback", "http://www.example.com/login/callback")]
    public void Routing_CanRenderPagesWithConstrainedParameters_And_TransitionToInteractive(string url, string expectedValue)
    {
        ExecuteRoutingTestCore(url, expectedValue);
    }

    [Theory]
    [InlineData("routing/complex-segment(value)", "value")]
    [InlineData("%F0%9F%99%82/routing/%F0%9F%99%82complex-segment(http%3A%2F%2Fwww.example.com%2Flogin%2Fcallback)", "http://www.example.com/login/callback")]
    public void Routing_CanRenderPagesWithComplexSegments_And_TransitionToInteractive(string url, string expectedValue)
    {
        ExecuteRoutingTestCore(url, expectedValue);
    }

    [Fact]
    public void Routing_CanRenderPagesWithParametersWithDefaultValue_And_TransitionToInteractive()
    {
        ExecuteRoutingTestCore("routing/defaults", "default");
    }

    [Fact]
    public void Routing_CanRenderPagesWithOptionalParameters_And_TransitionToInteractive()
    {
        ExecuteRoutingTestCore("routing/optional", "null");
    }

    [Theory]
    [InlineData("routing/catch-all/rest/of/the/path", "rest/of/the/path")]
    [InlineData("%F0%9F%99%82/routing/catch-all/http%3A%2F%2Fwww.example.com%2Flogin%2Fcallback/another", "http://www.example.com/login/callback/another")]
    public void Routing_CanRenderPagesWithCatchAllParameters_And_TransitionToInteractive(string url, string expectedValue)
    {
        ExecuteRoutingTestCore(url, expectedValue);
    }

    [Theory]
    [InlineData("routing/constrained-catch-all/a/b", "a/b")]
    [InlineData("%F0%9F%99%82/routing/constrained-catch-all/http%3A%2F%2Fwww.example.com%2Flogin%2Fcallback/another", "http://www.example.com/login/callback/another")]
    public void Routing_CanRenderPagesWithConstrainedCatchAllParameters_And_TransitionToInteractive(string url, string expectedValue)
    {
        ExecuteRoutingTestCore(url, expectedValue);
    }

    private void ExecuteRoutingTestCore(string url, string expectedValue)
    {
        GoTo($"{url}?suppress-autostart");

        Browser.Equal(expectedValue, () => Browser.FindElement(By.Id("parameter-value")).Text);

        Browser.Exists(By.Id("call-blazor-start"));

        Browser.Click(By.Id("call-blazor-start"));

        Browser.Exists(By.Id("interactive"));

        Browser.Equal(expectedValue, () => Browser.FindElement(By.Id("parameter-value")).Text);
    }

    private void GoTo(string relativePath)
    {
        Navigate($"{ServerPathBase}/{relativePath}");
    }
}
