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
        var imageDiv = Browser.FindElement(By.Id("png-basic-container"));

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
        var imageDiv = Browser.FindElement(By.Id("dynamic-source-container"));
        var imageElement = imageDiv.FindElement(By.TagName("img"));
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
}
