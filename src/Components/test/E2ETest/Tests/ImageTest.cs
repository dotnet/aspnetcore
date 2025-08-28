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

    private void ClearImageCache()
    {
        var ok = (bool)((IJavaScriptExecutor)Browser).ExecuteAsyncScript(@"
          var done = arguments[0];
          (async () => {
            try {
              if ('caches' in window) {
                await caches.delete('blazor-image-cache');
              }
              // Reset memoized cache promise if present
              try {
                const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
                if (root && 'cachePromise' in root) {
                  root.cachePromise = undefined;
                }
              } catch {}
              done(true);
            } catch (e) {
              done(false);
            }
          })();
        ");
        Assert.True(ok, "Failed to clear image cache");
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

        var marker = imageElement.GetAttribute("data-blazor-image");
        Assert.NotNull(marker);

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
    public void ErrorImage_SetsErrorState()
    {
        Browser.FindElement(By.Id("load-error")).Click();
        Browser.Equal("Error image loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var errorImg = Browser.FindElement(By.Id("error-image"));

        Browser.Equal("error", () => Browser.FindElement(By.Id("error-image")).GetAttribute("data-state"));
        var src = errorImg.GetAttribute("src");
        Assert.True(string.IsNullOrEmpty(src) || !src.StartsWith("blob:", StringComparison.Ordinal));
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
    public void Image_CompletesLoad_AfterArtificialDelay()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
              if (!root) return;
              if (!window.__origSetImageAsync) {
                window.__origSetImageAsync = root.setImageAsync;
                root.setImageAsync = async function(...args){
                  const getResolvers = () => {
                    if (Promise.fromResolvers) return Promise.fromResolvers();
                    let resolve, reject;
                    const promise = new Promise((r,j)=>{ resolve=r; reject=j; });
                    return { promise, resolve, reject };
                  };
                  const resolvers = getResolvers();
                  window.__imagePromiseResolvers = resolvers;
                  await resolvers.promise;
                  return window.__origSetImageAsync.apply(this, args);
                };
              }
            })();");

        Browser.FindElement(By.Id("load-png")).Click();

        var imageElement = Browser.FindElement(By.Id("png-basic"));
        Assert.NotNull(imageElement);

        ((IJavaScriptExecutor)Browser).ExecuteScript("if (window.__imagePromiseResolvers) { window.__imagePromiseResolvers.resolve(); }");

        Browser.True(() => {
            var src = imageElement.GetAttribute("src");
            return !string.IsNullOrEmpty(src) && src.StartsWith("blob:", StringComparison.Ordinal);
        });
        Browser.Equal("PNG basic loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        // Restore original function and clean up instrumentation
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
              if (root && window.__origSetImageAsync) {
                root.setImageAsync = window.__origSetImageAsync;
                delete window.__origSetImageAsync;
              }
              delete window.__imagePromiseResolvers;
            })();");
    }

    [Fact]
    public void ImageCache_PersistsAcrossPageReloads()
    {
        ClearImageCache();

        Browser.FindElement(By.Id("load-cached-jpg")).Click();
        Browser.Equal("Cached JPG loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        var firstImg = Browser.FindElement(By.Id("cached-jpg"));
        Browser.True(() => !string.IsNullOrEmpty(firstImg.GetAttribute("src")));
        var firstSrc = firstImg.GetAttribute("src");
        Assert.StartsWith("blob:", firstSrc, StringComparison.Ordinal);

        Browser.Navigate().Refresh();
        Navigate(ServerPathBase);
        Browser.MountTestComponent<ImageTestComponent>();

        // Re‑instrument after refresh so we see cache vs stream on the second load
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
              if (!root) return;
              window.__cacheHits = 0;
              window.__streamCalls = 0;
              if (!window.__origSetImageAsync){
                  window.__origSetImageAsync = root.setImageAsync;
                  root.setImageAsync = async function(...a){
                      const result = await window.__origSetImageAsync.apply(this, a);
                      if (result && result.fromCache) window.__cacheHits++;
                      if (result && result.success && !result.fromCache) window.__streamCalls++;
                      return result;
                  };
              }
            })();");

        // Second load should hit cache
        Browser.FindElement(By.Id("load-cached-jpg")).Click();
        Browser.Equal("Cached JPG loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        var secondImg = Browser.FindElement(By.Id("cached-jpg"));
        Browser.True(() => !string.IsNullOrEmpty(secondImg.GetAttribute("src")));
        var secondSrc = secondImg.GetAttribute("src");
        Assert.StartsWith("blob:", secondSrc, StringComparison.Ordinal);

        var hits = (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__cacheHits || 0;");
        var streamCalls = (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__streamCalls || 0;");

        Assert.Equal(1, hits);
        Assert.Equal(0, streamCalls);
        Assert.NotEqual(firstSrc, secondSrc);

        // Restore
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryImageComponent;
              if (root && window.__origSetImageAsync){ root.setImageAsync = window.__origSetImageAsync; delete window.__origSetImageAsync; }
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

        Assert.NotEqual(initialSrc, finalSrc);
    }

    [Fact]
    public void UrlRevoked_WhenImageRemovedFromDom()
    {
        // Load an image and capture its blob URL
        Browser.FindElement(By.Id("load-png")).Click();
        Browser.Equal("PNG basic loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        var imageElement = Browser.FindElement(By.Id("png-basic"));
        var blobUrl = imageElement.GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(blobUrl));
        Assert.StartsWith("blob:", blobUrl, StringComparison.Ordinal);

        // MutationObserver should revoke the URL
        ((IJavaScriptExecutor)Browser).ExecuteScript("document.getElementById('png-basic').remove();");

        // Poll until fetch fails, indicating the URL has been revoked
        Browser.True(() =>
        {
            try
            {
                var ok = (bool)((IJavaScriptExecutor)Browser).ExecuteAsyncScript(@"
                  var callback = arguments[arguments.length - 1];
                  var url = arguments[0];
                  (async () => {
                    try {
                      await fetch(url);
                      callback(false); // still reachable
                    } catch {
                      callback(true); // revoked or unreachable
                    }
                  })();
                ", blobUrl);
                return ok;
            }
            catch
            {
                return false;
            }
        });
    }

    [Fact]
    public void InvalidMimeImage_SetsErrorState()
    {
        Browser.FindElement(By.Id("load-invalid-mime")).Click();
        Browser.Equal("Invalid mime image loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var img = Browser.FindElement(By.Id("invalid-mime-image"));
        Assert.NotNull(img);

        Browser.Equal("error", () => img.GetAttribute("data-state"));

        var src = img.GetAttribute("src");
        Assert.True(string.IsNullOrEmpty(src) || src.StartsWith("blob:", StringComparison.Ordinal));
    }
}
