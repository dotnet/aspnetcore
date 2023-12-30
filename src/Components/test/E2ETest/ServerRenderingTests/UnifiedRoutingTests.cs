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

    [Fact]
    public void Routing_CanRenderPagesWithParameters_And_TransitionToInteractive()
    {
        ExecuteRoutingTestCore("routing/parameters/value", "value");
    }

    [Fact]
    public void Routing_CanRenderPagesWithConstrainedParameters_And_TransitionToInteractive()
    {
        ExecuteRoutingTestCore("routing/constraints/5", "5");
    }

    [Fact]
    public void Routing_CanRenderPagesWithComplexSegments_And_TransitionToInteractive()
    {
        ExecuteRoutingTestCore("routing/complex-segment(value)", "value");
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

    [Fact]
    public void Routing_CanRenderPagesWithCatchAllParameters_And_TransitionToInteractive()
    {
        ExecuteRoutingTestCore("routing/catch-all/rest/of/the/path", "rest/of/the/path");
    }

    [Fact]
    public void Routing_CanRenderPagesWithConstrainedCatchAllParameters_And_TransitionToInteractive()
    {
        ExecuteRoutingTestCore("routing/constrained-catch-all/a/b", "a/b");
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
