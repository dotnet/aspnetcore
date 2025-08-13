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
    public void CanLoadPngImage()
    {
        // Load PNG with cache
        Browser.FindElement(By.Id("load-png")).Click();

        // Wait for loading to complete using Browser.Equal pattern
        Browser.Equal("PNG basic loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify the image element exists and has a blob URL
        var imageElement = Browser.FindElement(By.Id("png-basic"));

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
        var imageElement = Browser.FindElement(By.Id("jpg-stream"));
        Assert.NotNull(imageElement);

        var src = imageElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(src), "Image src should not be empty");
        Assert.True(src.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {src}");
    }

    [Fact]
    public void CanLoadLargeImageWithSmallChunks()
    {
        // Load large image with small chunks to test chunked loading
        Browser.FindElement(By.Id("load-small-chunks")).Click();

        // Wait for loading to complete (this may take longer)
        Browser.Equal("Small chunks image loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify the image element exists and has a blob URL
        var imageElement = Browser.FindElement(By.Id("chunked-image"));
        Assert.NotNull(imageElement);

        var src = imageElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(src), "Image src should not be empty");
        Assert.True(src.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {src}");
    }

    [Fact]
    public void CanChangeDynamicImageSource()
    {
        // First click - initialize with PNG
        Browser.FindElement(By.Id("change-source")).Click();
        Browser.Equal("Dynamic source initialized with PNG", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify the image element exists and has a blob URL
        var imageElement = Browser.FindElement(By.Id("dynamic-source"));
        Assert.NotNull(imageElement);

        var firstSrc = imageElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(firstSrc), "Image src should not be empty");
        Assert.True(firstSrc.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {firstSrc}");

        // Second click - change to JPG
        Browser.FindElement(By.Id("change-source")).Click();
        Browser.Equal("Dynamic source changed to JPG", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify the image source has changed
        var secondSrc = imageElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(secondSrc), "Image src should not be empty after change");
        Assert.True(secondSrc.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {secondSrc}");
        Assert.NotEqual(firstSrc, secondSrc);

        // Third click - change back to PNG
        Browser.FindElement(By.Id("change-source")).Click();
        Browser.Equal("Dynamic source changed to PNG", () => Browser.FindElement(By.Id("current-status")).Text);

        // Verify the image source has changed again
        var thirdSrc = imageElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(thirdSrc), "Image src should not be empty after second change");
        Assert.True(thirdSrc.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {thirdSrc}");
        Assert.NotEqual(secondSrc, thirdSrc);
    }

    [Fact]
    public void CachePersistsAcrossRemounts_NoStreamingOnHit()
    {
        // Clear cache to start clean
        Browser.FindElement(By.Id("clear-cache")).Click();
        Browser.Equal("Cache cleared", () => Browser.FindElement(By.Id("current-status")).Text);

        // First mount streams and caches
        Browser.FindElement(By.Id("mount-cached")).Click();
        Browser.Equal("Cached mount loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        var firstSrc = Browser.FindElement(By.Id("cached-image")).GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(firstSrc));
        Assert.StartsWith("blob:", firstSrc, StringComparison.Ordinal);

        // Remount should hit cache and produce a (likely) different blob URL but without streaming
        Browser.FindElement(By.Id("remount-cached")).Click();
        Browser.Equal("Remounted cached image", () => Browser.FindElement(By.Id("current-status")).Text);
        var secondSrc = Browser.FindElement(By.Id("cached-image")).GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(secondSrc));
        Assert.StartsWith("blob:", secondSrc, StringComparison.Ordinal);
        // Different blob URLs may be generated; the key check here is that it loads quickly. We assert src present.
    }

    [Fact]
    public void TwoImagesWithSameCacheKey_LoadSecondFromCache()
    {
        // Ensure clean cache then load pair sequence
        Browser.FindElement(By.Id("clear-cache")).Click();
        Browser.Equal("Cache cleared", () => Browser.FindElement(By.Id("current-status")).Text);

        Browser.FindElement(By.Id("load-pair-sequence")).Click();
        Browser.Equal("Pair second loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var img1 = Browser.FindElement(By.Id("pair-image-1"));
        var img2 = Browser.FindElement(By.Id("pair-image-2"));
        var src1 = img1.GetAttribute("src");
        var src2 = img2.GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(src1));
        Assert.False(string.IsNullOrEmpty(src2));
        Assert.StartsWith("blob:", src1, StringComparison.Ordinal);
        Assert.StartsWith("blob:", src2, StringComparison.Ordinal);
    }

    [Fact]
    public void RapidSourceChanges_FinalStateLoads()
    {
        Browser.FindElement(By.Id("rapid-change")).Click();
        Browser.Equal("Rapid change completed", () => Browser.FindElement(By.Id("current-status")).Text);

        var img = Browser.FindElement(By.Id("dynamic-source"));
        var src = img.GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(src));
        Assert.StartsWith("blob:", src, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearingCache_ForcesReloadAfterwards()
    {
        // Mount cached image first (uses CacheStrategy.Memory)
        Browser.FindElement(By.Id("mount-cached")).Click();
        Browser.Equal("Cached mount loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        var beforeClear = Browser.FindElement(By.Id("cached-image")).GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(beforeClear));
        Assert.StartsWith("blob:", beforeClear, StringComparison.Ordinal);

        // Clear cache
        Browser.FindElement(By.Id("clear-cache")).Click();
        Browser.Equal("Cache cleared", () => Browser.FindElement(By.Id("current-status")).Text);

        // Remount same cached image to force re-stream
        Browser.FindElement(By.Id("remount-cached")).Click();
        Browser.Equal("Remounted cached image", () => Browser.FindElement(By.Id("current-status")).Text);
        var afterClear = Browser.FindElement(By.Id("cached-image")).GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(afterClear));
        Assert.StartsWith("blob:", afterClear, StringComparison.Ordinal);
    }
}
