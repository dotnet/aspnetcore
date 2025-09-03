// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.MediaTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class VideoTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public VideoTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<VideoTestComponent>();
    }

    private void ClearMediaCache()
    {
        var ok = (bool)((IJavaScriptExecutor)Browser).ExecuteAsyncScript(@"
          var done = arguments[0];
          (async () => {
            try {
              if ('caches' in window) {
                await caches.delete('blazor-media-cache');
              }
              try {
                const root = Blazor && Blazor._internal && Blazor._internal.BinaryMedia;
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
        Assert.True(ok, "Failed to clear media cache");
    }

    [Fact]
    public void CanLoadMp4Video()
    {
        Browser.FindElement(By.Id("load-mp4")).Click();

        // Wait for status update
        Browser.Equal("MP4 basic loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var video = Browser.FindElement(By.Id("mp4-basic"));
        Assert.NotNull(video);

        Browser.True(() =>
        {
            var src = video.GetAttribute("src");
            return !string.IsNullOrEmpty(src) && src.StartsWith("blob:", StringComparison.Ordinal);
        });

        var marker = video.GetAttribute("data-blazor-video");
        Assert.NotNull(marker);

        var state = video.GetAttribute("data-state");
        // After load state should be cleared (null or empty)
        Assert.True(string.IsNullOrEmpty(state));
    }

    [Fact]
    public void CanChangeDynamicVideoSource()
    {
        Browser.FindElement(By.Id("change-video")).Click();
        Browser.Equal("Dynamic video initialized (A)", () => Browser.FindElement(By.Id("current-status")).Text);
        var video = Browser.FindElement(By.Id("dynamic-video"));
        Browser.True(() => !string.IsNullOrEmpty(video.GetAttribute("src")) && video.GetAttribute("src").StartsWith("blob:", StringComparison.Ordinal));
        var firstSrc = video.GetAttribute("src");

        Browser.FindElement(By.Id("change-video")).Click();
        Browser.Equal("Dynamic video changed to B", () => Browser.FindElement(By.Id("current-status")).Text);
        Browser.True(() =>
        {
            var s = video.GetAttribute("src");
            return !string.IsNullOrEmpty(s) && s.StartsWith("blob:", StringComparison.Ordinal) && s != firstSrc;
        });
        var secondSrc = video.GetAttribute("src");

        Browser.FindElement(By.Id("change-video")).Click();
        Browser.Equal("Dynamic video changed to A", () => Browser.FindElement(By.Id("current-status")).Text);
        Browser.True(() =>
        {
            var s = video.GetAttribute("src");
            return !string.IsNullOrEmpty(s) && s.StartsWith("blob:", StringComparison.Ordinal) && s != secondSrc;
        });
    }

    [Fact]
    public void ErrorVideo_SetsErrorState()
    {
        Browser.FindElement(By.Id("load-error-video")).Click();
        Browser.Equal("Error video loaded", () => Browser.FindElement(By.Id("current-status")).Text);
        var video = Browser.FindElement(By.Id("error-video"));
        Browser.Equal("error", () => video.GetAttribute("data-state"));
        var src = video.GetAttribute("src");
        Assert.True(string.IsNullOrEmpty(src) || !src.StartsWith("blob:", StringComparison.Ordinal));
    }

    [Fact]
    public void VideoCache_PersistsAcrossPageReloads()
    {
        ClearMediaCache();

        // First load (stream)
        Browser.FindElement(By.Id("load-mp4")).Click();
        var video = Browser.FindElement(By.Id("mp4-basic"));
        Browser.True(() => !string.IsNullOrEmpty(video.GetAttribute("src")));
        var firstSrc = video.GetAttribute("src");
        Assert.StartsWith("blob:", firstSrc, StringComparison.Ordinal);

        // Refresh the page to force a new circuit / JS context
        Browser.Navigate().Refresh();
        Navigate(ServerPathBase);
        Browser.MountTestComponent<VideoTestComponent>();

        // Instrument setContentAsync AFTER refresh so instrumentation remains for second load
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryMedia;
              if (!root) return;
              window.__cacheHits = 0; window.__streamCalls = 0;
              if (!window.__origSetContentAsync){
                window.__origSetContentAsync = root.setContentAsync;
                root.setContentAsync = async function(...a){
                  const r = await window.__origSetContentAsync.apply(this, a);
                  if (r && r.fromCache) window.__cacheHits++; else if (r && r.success && !r.fromCache) window.__streamCalls++;
                  return r;
                };
              }
            })();
        ");

        // Second load should hit cache
        Browser.FindElement(By.Id("load-mp4")).Click();
        var video2 = Browser.FindElement(By.Id("mp4-basic"));
        Browser.True(() => !string.IsNullOrEmpty(video2.GetAttribute("src")));
        var secondSrc = video2.GetAttribute("src");
        Assert.StartsWith("blob:", secondSrc, StringComparison.Ordinal);

        var hits = (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__cacheHits || 0;");
        var streams = (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__streamCalls || 0;");
        Assert.Equal(1, hits);
        Assert.Equal(0, streams);
        Assert.NotEqual(firstSrc, secondSrc);

        // Restore
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryMedia;
              if (root && window.__origSetContentAsync){ root.setContentAsync = window.__origSetContentAsync; delete window.__origSetContentAsync; }
              delete window.__cacheHits; delete window.__streamCalls;
            })();
        ");
    }

    [Fact]
    public void RapidVideoSourceChanges_MaintainsConsistency()
    {
        Browser.FindElement(By.Id("change-video")).Click();
        Browser.Equal("Dynamic video initialized (A)", () => Browser.FindElement(By.Id("current-status")).Text);
        var video = Browser.FindElement(By.Id("dynamic-video"));
        Browser.True(() => !string.IsNullOrEmpty(video.GetAttribute("src")));
        var initialSrc = video.GetAttribute("src");

        for (int i = 0; i < 10; i++)
        {
            Browser.FindElement(By.Id("change-video")).Click();
        }

        Browser.True(() =>
        {
            var status = Browser.FindElement(By.Id("current-status")).Text;
            var src = video.GetAttribute("src");
            var state = video.GetAttribute("data-state");
            if (string.IsNullOrEmpty(src) || !src.StartsWith("blob:", StringComparison.Ordinal))
            {
                return false;
            }

            if (state == "loading" || state == "error")
            {
                return false;
            }

            return status.Contains("Dynamic video changed to B") || status.Contains("Dynamic video changed to A");
        });

        var finalSrc = video.GetAttribute("src");
        Assert.False(string.IsNullOrEmpty(finalSrc));
        Assert.StartsWith("blob:", finalSrc, StringComparison.Ordinal);

        Assert.NotEqual(initialSrc, finalSrc);
    }

    [Fact]
    public void UrlRevoked_WhenVideoRemovedFromDom()
    {
        Browser.FindElement(By.Id("load-mp4")).Click();
        var video = Browser.FindElement(By.Id("mp4-basic"));
        Browser.True(() => !string.IsNullOrEmpty(video.GetAttribute("src")));
        var blobUrl = video.GetAttribute("src");
        Assert.StartsWith("blob:", blobUrl, StringComparison.Ordinal);

        // Remove element
        ((IJavaScriptExecutor)Browser).ExecuteScript("document.getElementById('mp4-basic').remove();");

        // Poll until fetch fails
        Browser.True(() =>
        {
            try
            {
                var ok = (bool)((IJavaScriptExecutor)Browser).ExecuteAsyncScript(@"
                  var callback = arguments[arguments.length - 1];
                  var url = arguments[0];
                  (async () => {
                    try { await fetch(url); callback(false); } catch { callback(true); }
                  })();
                ", blobUrl);
                return ok;
            }
            catch { return false; }
        });
    }
}
