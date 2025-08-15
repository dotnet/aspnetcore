// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BasicTestApp;
using BasicTestApp.ImageTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
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
        Browser.FindElement(By.Id("load-png")).Click();

        Browser.Equal("PNG basic loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var imageElement = Browser.FindElement(By.Id("png-basic"));

        Assert.NotNull(imageElement);

        var src = imageElement.GetAttribute("src");
        Assert.True(!string.IsNullOrEmpty(src), "Image src should not be empty");
        Assert.True(src.StartsWith("blob:", StringComparison.Ordinal), $"Expected blob URL, but got: {src}");

        var state = imageElement.GetAttribute("data-state");
        Assert.True(string.IsNullOrEmpty(state), $"Expected data-state to be cleared after load, but found '{state}'");
    }

    [Fact]
    public void CanLoadJpgImageFromStream()
    {
        Browser.FindElement(By.Id("load-jpg-stream")).Click();

        Browser.Equal("JPG from stream loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var imageElement = Browser.FindElement(By.Id("jpg-stream"));
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
    public void TwoImagesWithSameCacheKey_LoadSecondFromCache()
    {
        // IMPORTANT: This test deliberately reuses the SAME ImageSource instance for two <Image> components.
        // ImageSource is documented as single-use.
        // The reason this still passes today is that the second component never streams the data:
        //   1. First image probes cache (miss) then streams bytes, advancing the MemoryStream to end and
        //      populating the browser Cache API under the shared cache key.
        //   2. Second image (same cache key, same ImageSource whose stream is now at end) probes cache and
        //      gets a HIT. On a cache hit the component SHORT-CIRCUITS and skips StreamImage(), so the
        //      position check that would detect a consumed stream is never executed.
        // If we later enforce single-use before the streaming method (e.g. checking stream state
        // before the cache probe) this test should be rewritten.

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
    public void ErrorImage_ShowsErrorState()
    {
        // Trigger loading of an image whose stream position is not at start, causing error
        Browser.FindElement(By.Id("load-error")).Click();
        Browser.Equal("Error image loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var errorImg = Browser.FindElement(By.Id("error-image"));
        var state = errorImg.GetAttribute("data-state");
        Assert.Equal("error", state);
    }

    [Fact]
    public void ImageRenders_WithCorrectDimensions()
    {
        Browser.FindElement(By.Id("load-png")).Click();
        Browser.Equal("PNG basic loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var imageElement = Browser.FindElement(By.Id("png-basic"));

        // Wait for actual dimensions to be set
        Browser.True(() =>
        {
            var width = imageElement.GetAttribute("naturalWidth");
            return !string.IsNullOrEmpty(width) && int.Parse(width, CultureInfo.InvariantCulture) > 0;
        });

        var naturalWidth = int.Parse(imageElement.GetAttribute("naturalWidth"), CultureInfo.InvariantCulture);
        var naturalHeight = int.Parse(imageElement.GetAttribute("naturalHeight"), CultureInfo.InvariantCulture);

        Assert.Equal(1, naturalWidth);
        Assert.Equal(1, naturalHeight);
    }

    [Fact]
    public void Image_ShowsLoadingStateThenCompletes()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
              if (!root) return;
              if (!window.__origLoadImage) {
                window.__origLoadImage = root.loadImageFromStream;
                root.loadImageFromStream = async function(...args){
                  await new Promise(r => setTimeout(r, 500));
                  return window.__origLoadImage.apply(this, args);
                };
              }
            })();");

        Browser.FindElement(By.Id("load-png")).Click();

        Browser.True(() =>
        {
            try { Browser.FindElement(By.Id("png-basic")); return true; } catch { return false; }
        });

        var imageElement = Browser.FindElement(By.Id("png-basic"));
        Browser.Equal("loading", () => imageElement.GetAttribute("data-state"));
        Browser.Equal("PNG basic loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        Browser.Equal(null, () => imageElement.GetAttribute("data-state"));
        Browser.True(() =>
        {
            var src = imageElement.GetAttribute("src");
            return !string.IsNullOrEmpty(src) && src.StartsWith("blob:", StringComparison.Ordinal);
        });

        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
              if (root && window.__origLoadImage) {
                root.loadImageFromStream = window.__origLoadImage;
                delete window.__origLoadImage;
              }
            })();");
    }

    [Fact]
    public void ImageCache_PersistsAcrossPageReloads()
    {
        Browser.FindElement(By.Id("clear-cache")).Click();
        Browser.Equal("Cache cleared", () => Browser.FindElement(By.Id("current-status")).Text);

        // First load (streams, then caches)
        Browser.FindElement(By.Id("load-cached-jpg")).Click();
        Browser.Equal("Cached JPG loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        var firstImg = Browser.FindElement(By.Id("cached-jpg"));
        Browser.True(() => !string.IsNullOrEmpty(firstImg.GetAttribute("src")));
        var firstSrc = firstImg.GetAttribute("src");
        Assert.StartsWith("blob:", firstSrc, StringComparison.Ordinal);

        // Refresh page (loses any prior instrumentation)
        Browser.Navigate().Refresh();
        Navigate(ServerPathBase);
        Browser.MountTestComponent<ImageTestComponent>();

        // Re‑instrument after refresh so we see the cache probe on the second load
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
            const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
            if (!root) return;
            // Reset counters
            window.__cacheHits = 0;
            window.__streamCalls = 0;
            if (!window.__origTrySet){
                window.__origTrySet = root.trySetFromCache;
                root.trySetFromCache = async function(img, key){
                const r = await window.__origTrySet.call(this, img, key);
                if (r) window.__cacheHits++;
                return r;
                };
            }
            if (!window.__origLoadStream){
                window.__origLoadStream = root.loadImageFromStream;
                root.loadImageFromStream = async function(...a){
                window.__streamCalls++;
                return window.__origLoadStream.apply(this, a);
                };
            }
            })();");

        // Second load should hit cache (no streaming)
        Browser.FindElement(By.Id("load-cached-jpg")).Click();
        Browser.Equal("Cached JPG loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        var secondImg = Browser.FindElement(By.Id("cached-jpg"));
        Browser.True(() => !string.IsNullOrEmpty(secondImg.GetAttribute("src")));
        var secondSrc = secondImg.GetAttribute("src");
        Assert.StartsWith("blob:", secondSrc, StringComparison.Ordinal);

        // Read counters
        var hits = (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__cacheHits || 0;");
        var streamCalls = (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__streamCalls || 0;");

        Assert.Equal(1, hits);
        Assert.Equal(0, streamCalls);
        Assert.NotEqual(firstSrc, secondSrc); // New blob URL created from cached bytes

        // Cleanup instrumentation
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
            const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
            if (root && window.__origTrySet){ root.trySetFromCache = window.__origTrySet; delete window.__origTrySet; }
            if (root && window.__origLoadStream){ root.loadImageFromStream = window.__origLoadStream; delete window.__origLoadStream; }
            delete window.__cacheHits;
            delete window.__streamCalls;
            })();");
    }

    [Fact]
    public void RapidSourceChanges_MaintainsConsistency()
    {
        // Initialize dynamic image
        Browser.FindElement(By.Id("change-source")).Click();
        Browser.Equal("Dynamic source initialized with PNG", () => Browser.FindElement(By.Id("current-status")).Text);

        var imageElement = Browser.FindElement(By.Id("dynamic-source"));
        Browser.True(() => !string.IsNullOrEmpty(imageElement.GetAttribute("src")));
        var initialSrc = imageElement.GetAttribute("src");

        // Simulate user quickly clicking
        for (int i = 0; i < 10; i++)
        {
            Browser.FindElement(By.Id("change-source")).Click();
        }

        // Wait until status shows a completed PNG or JPG change and image has a blob src
        Browser.True(() =>
        {
            var status = Browser.FindElement(By.Id("current-status")).Text;
            var src = imageElement.GetAttribute("src");
            var state = imageElement.GetAttribute("data-state");
            if (string.IsNullOrEmpty(src) || !src.StartsWith("blob:", StringComparison.Ordinal))
            {
                return false;
            }

            if (state == "loading" || state == "error")
            {
                return false;
            }

            return status.Contains("Dynamic source changed to PNG") || status.Contains("Dynamic source changed to JPG");
        });

        var finalSrc = imageElement.GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(finalSrc));
        Assert.StartsWith("blob:", finalSrc, StringComparison.Ordinal);
        // After multiple toggles we expect a change in src
        Assert.NotEqual(initialSrc, finalSrc);
    }
}
