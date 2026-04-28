// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: Most shared media component behaviors (caching, error handling and URL revocation)
// are validated in ImageTest. To avoid duplication, this suite intentionally contains only
// tests that exercise <video> specific scenarios.

using BasicTestApp;
using BasicTestApp.MediaTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;
using System.Globalization;

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

    [Fact]
    public void CanLoadMp4Video()
    {
        Browser.FindElement(By.Id("load-mp4")).Click();

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
        Assert.True(string.IsNullOrEmpty(state));
    }

    [Fact]
    public void VideoRenders_WithCorrectDimensions()
    {
        Browser.FindElement(By.Id("load-mp4")).Click();
        Browser.Equal("MP4 basic loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var video = Browser.FindElement(By.Id("mp4-basic"));
        Assert.NotNull(video);

        // Wait for dimensions (videoWidth/videoHeight) to be available (> 0)
        Browser.True(() =>
        {
            var widthStr = video.GetAttribute("videoWidth");
            return !string.IsNullOrEmpty(widthStr) && int.Parse(widthStr, CultureInfo.InvariantCulture) > 0;
        });

        var videoWidth = int.Parse(video.GetAttribute("videoWidth"), CultureInfo.InvariantCulture);
        var videoHeight = int.Parse(video.GetAttribute("videoHeight"), CultureInfo.InvariantCulture);

        // Expected dimensions 1024x1024
        Assert.Equal(1024, videoWidth);
        Assert.Equal(1024, videoHeight);
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
        var state = video.GetAttribute("data-state");

        Assert.False(string.IsNullOrEmpty(finalSrc));
        Assert.True(string.IsNullOrEmpty(state));
        Assert.StartsWith("blob:", finalSrc, StringComparison.Ordinal);

        Assert.NotEqual(initialSrc, finalSrc);
    }

    [Fact]
    public void TemplatedVideo_LoadsSuccessfully()
    {
        Browser.FindElement(By.Id("load-templated-video")).Click();
        Browser.Equal("Templated video loaded", () => Browser.FindElement(By.Id("current-status")).Text);

        var container = Browser.FindElement(By.Id("templated-video-container"));
        Assert.NotNull(container);
        var video = Browser.FindElement(By.Id("templated-video"));
        Browser.True(() =>
        {
            var src = video.GetAttribute("src");
            return !string.IsNullOrEmpty(src) && src.StartsWith("blob:", StringComparison.Ordinal);
        });
        var status = Browser.FindElement(By.Id("templated-video-status")).Text;
        Assert.Equal("Loaded", status);
        var cls = container.GetAttribute("class");
        Assert.Contains("video-shell", cls);
        Assert.Contains("ready", cls);
    }
}
