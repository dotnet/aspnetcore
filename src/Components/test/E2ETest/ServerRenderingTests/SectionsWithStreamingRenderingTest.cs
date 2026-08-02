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

public class SectionsWithStreamingRenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public SectionsWithStreamingRenderingTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void StreamingRenderingForSectionOutletContentIsDeterminedByMatchingSectionContent()
    {
        Navigate($"{ServerPathBase}/sections-with-streaming");

        Browser.Equal("Sections with Streaming", () => Browser.Exists(By.TagName("h1")).Text);

        // Second SectionContent overrides the content and StreamingRendering attribute for SectionOutlet

        Browser.DoesNotExist(By.Id("first-section-content"));

        CanPerformStreamingRendering();
    }

    private void CanPerformStreamingRendering()
    {
        // Initial "waiting" state
        var getStatusText = () => Browser.Exists(By.Id("status"));
        var getDisplayedItems = () => Browser.FindElements(By.TagName("li"));
        Assert.Equal("Waiting for more...", getStatusText().Text);
        Assert.Empty(getDisplayedItems());

        // Can add items
        for (var i = 1; i <= 3; i++)
        {
            // Each time we click, there's another streaming render batch and the UI is updated
            Browser.FindElement(By.Id("add-item-link")).Click();
            Browser.Collection(getDisplayedItems, Enumerable.Range(1, i).Select<int, Action<IWebElement>>(index =>
            {
                return actualItem => Assert.Equal($"Item {index}", actualItem.Text);
            }).ToArray());
            Assert.Equal("Waiting for more...", getStatusText().Text);

            // These are insta-removed so they don't pollute anything
            Browser.DoesNotExist(By.TagName("blazor-ssr"));
        }

        // Can finish the response
        Browser.FindElement(By.Id("end-response-link")).Click();
        Browser.Equal("Finished", () => getStatusText().Text);
    }
}
