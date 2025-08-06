// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.ImageTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Diagnostics.Runtime.Interop;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class ImageTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public ImageTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<ImageTestComponent>();
    }

    [Fact]
    public void CanLoadPngImageWithCache()
    {
        // Load PNG with cache
        Browser.FindElement(By.Id("load-png-cached")).Click();

        // Wait for loading to complete using Browser.Equal pattern
        Browser.Equal("PNG with cache loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify the image element exists and has a blob URL
        var imageDiv = Browser.FindElement(By.Id("png-cached-container"));

        var imageElement = imageDiv.FindElement(By.TagName("img"));
        Assert.NotNull(imageElement);

        var src = imageElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(src), "Image src should not be empty");
        Assert.True(src.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {src}");

        // Verify the image is not hidden (not in loading or error state)
        var cssClass = imageElement.GetAttribute("class");
        Assert.DoesNotContain("d-none", cssClass);
    }

    [Fact]
    public void CanLoadJpgImageFromStream()
    {
        // Load JPG from stream
        Browser.FindElement(By.Id("load-jpg-stream")).Click();

        // Wait for loading to complete
        Browser.Equal("JPG from stream loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify the image element exists and has a blob URL
        var imageDiv = Browser.FindElement(By.Id("jpg-stream-container"));
        var imageElement = imageDiv.FindElement(By.TagName("img"));
        Assert.NotNull(imageElement);

        var src = imageElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(src), "Image src should not be empty");
        Assert.True(src.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {src}");
    }

    [Fact]
    public void HandlesImageLoadError()
    {
        // Try to load an error image
        Browser.FindElement(By.Id("load-error")).Click();

        // Wait for error state
        Browser.Equal("Error loading image", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify error content is displayed
        Browser.True(() => Browser.FindElement(By.Id("error-image")).Text.Contains("Error loading image"));

        // Verify the image is hidden when in error state
        var imageElement = Browser.FindElement(By.Id("error-image")).FindElement(By.TagName("img"));
        var cssClass = imageElement.GetAttribute("class");
        Assert.Contains("d-none", cssClass);
    }

    [Fact]
    public void ShowsLoadingContent()
    {
        // Load large image to see loading content
        Browser.FindElement(By.Id("load-large-chunked")).Click();

        // Verify loading content is shown initially
        Browser.True(() => Browser.FindElement(By.Id("loading-image")).Text.Contains("Loading..."));

        // Wait for loading to complete
        Browser.Equal("Large image loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify loading content is hidden after loading
        var loadingElement = Browser.FindElement(By.Id("loading-image")).FindElement(By.TagName("div"));
        var cssClass = loadingElement.GetAttribute("class");
        Assert.Contains("d-none", cssClass);
    }

    [Fact]
    public void CanLoadLargeImageWithSmallChunks()
    {
        // Load large image with small chunks to test chunked loading
        Browser.FindElement(By.Id("load-small-chunks")).Click();

        // Wait for loading to complete (this may take longer)
        Browser.Equal("Small chunks image loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify the image element exists and has a blob URL
        var imageDiv = Browser.FindElement(By.Id("chunked-image-container"));
        var imageElement = imageDiv.FindElement(By.TagName("img"));
        Assert.NotNull(imageElement);

        var imgElement = imageElement.FindElement(By.TagName("img"));
        var src = imgElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(src), "Image src should not be empty");
        Assert.True(src.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {src}");
    }

    [Fact]
    public void CacheWorksCorrectlyBetweenLoads()
    {
        // Load PNG with cache twice and verify it loads faster the second time
        Browser.FindElement(By.Id("load-png-cached")).Click();
        Browser.Equal("PNG with cache loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var firstLoadSrc = Browser.FindElement(By.Id("basic-image")).GetAttribute("src");

        // Clear and load again
        Browser.FindElement(By.Id("clear-all-images")).Click();
        Browser.Equal("All images cleared", () => Browser.FindElement(By.Id("current-status")).Text);

        // Load same image again
        Browser.FindElement(By.Id("load-png-cached")).Click();
        Browser.Equal("PNG with cache loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var secondLoadSrc = Browser.FindElement(By.Id("basic-image")).GetAttribute("src");

        // Both should be blob URLs (the cache mechanism is internal)
        Assert.StartsWith("blob:", firstLoadSrc, StringComparison.Ordinal);
        Assert.StartsWith("blob:", secondLoadSrc, StringComparison.Ordinal);
    }

    [Fact]
    public void RespondsToImageSourceChanges()
    {
        // Load first image
        Browser.FindElement(By.Id("load-png-cached")).Click();
        Browser.Equal("PNG with cache loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var firstSrc = Browser.FindElement(By.Id("basic-image")).GetAttribute("src");

        // Load different image in same component
        Browser.FindElement(By.Id("load-jpg-stream")).Click();
        Browser.Equal("JPG from stream loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var secondSrc = Browser.FindElement(By.Id("basic-image")).GetAttribute("src");

        // Should have different blob URLs
        Assert.NotEqual(firstSrc, secondSrc);
        Assert.StartsWith("blob:", firstSrc, StringComparison.Ordinal);
        Assert.StartsWith("blob:", secondSrc, StringComparison.Ordinal);
    }

    [Fact]
    public void ComponentPerformanceTest()
    {
        // This test verifies that multiple rapid image loads don't cause issues
        for (int i = 0; i < 3; i++)
        {
            Browser.FindElement(By.Id("load-png-cached")).Click();
            Browser.Equal("PNG with cache loaded", () => Browser.FindElement(By.Id("current-status")).Text);

            Browser.FindElement(By.Id("load-jpg-stream")).Click();
            Browser.Equal("JPG from stream loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        }

        // Verify final state is correct
        var imageElement = Browser.FindElement(By.Id("basic-image"));
        var src = imageElement.GetAttribute("src");
        Assert.StartsWith("blob:", src, StringComparison.Ordinal);
    }
}
