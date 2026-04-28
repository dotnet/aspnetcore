// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using BasicTestApp;
using BasicTestApp.MediaTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit;
using Xunit.Abstractions;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class FileDownloadTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public FileDownloadTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<FileDownloadTestComponent>();
    }

    private void InstrumentDownload()
    {
        var success = ((IJavaScriptExecutor)Browser).ExecuteAsyncScript(@"
            var callback = arguments[arguments.length - 1];
            (function(){
                if (window.__downloadInstrumentationStarted){ callback(true); return; }
                window.__downloadInstrumentationStarted = true;
                function tryPatch(){
                    const root = Blazor && Blazor._internal && Blazor._internal.BinaryMedia;
                    if (!root || !root.downloadAsync){ setTimeout(tryPatch, 50); return; }
                    if (!window.__origDownloadAsync){
                        window.__origDownloadAsync = root.downloadAsync;
                        window.__downloadCalls = 0;
                        window.__lastFileName = null;
                        root.downloadAsync = async function(...a){
                            window.__downloadCalls++;
                            // downloadAsync(element, streamRef, mimeType, totalBytes, fileName)
                            window.__lastFileName = a[4]; // fileName index
                            if (window.__forceErrorFileName && a[4] === window.__forceErrorFileName){
                                return false; // simulate failure
                            }
                            return window.__origDownloadAsync.apply(this, a);
                        };
                    }
                    callback(true);
                }
                tryPatch();
            })();
        ") is true;
        Assert.True(success, "Failed to instrument downloadAsync");
        Thread.Sleep(100);
    }

    private int GetDownloadCallCount() => Convert.ToInt32(((IJavaScriptExecutor)Browser).ExecuteScript("return window.__downloadCalls || 0;"), CultureInfo.InvariantCulture);

    private string? GetLastFileName() => (string?)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__lastFileName || null;");

    [Fact]
    public void InitialRender_DoesNotStartDownload()
    {
        InstrumentDownload();
        // Component rendered but no download link shown until button clicked
        Assert.Equal(0, GetDownloadCallCount());
    }

    [Fact]
    public void Click_InitiatesDownload()
    {
        InstrumentDownload();
        Browser.FindElement(By.Id("show-download")).Click();
        var link = Browser.FindElement(By.Id("download-link"));
        link.Click();
        Browser.True(() => GetDownloadCallCount() >= 1);
        Browser.True(() => GetLastFileName() == "test.png");
        Assert.Null(link.GetAttribute("data-state")); // no error or loading after completion
    }

    [Fact]
    public void BlankFileName_SuppressesDownload()
    {
        InstrumentDownload();
        Browser.FindElement(By.Id("show-blank-filename")).Click();
        var link = Browser.FindElement(By.Id("blank-download-link"));
        link.Click();
        // Should not invoke JS because filename blank. Wait briefly to ensure no async call occurs.
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < TimeSpan.FromMilliseconds(200))
        {
            Assert.True(GetDownloadCallCount() == 0, "Download should not have started for blank filename.");
            Thread.Sleep(20);
        }
        Assert.Equal(0, GetDownloadCallCount());
    }

    [Fact]
    public void ErrorDownload_SetsErrorState()
    {
        InstrumentDownload();
        // Force simulated failure via instrumentation hook
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.__forceErrorFileName='error.txt';");
        Browser.FindElement(By.Id("show-error-download")).Click();
        var link = Browser.FindElement(By.Id("error-download-link"));
        link.Click();
        Browser.Equal("error", () => link.GetAttribute("data-state"));
    }

    [Fact]
    public void ProvidedHref_IsRemoved_AndInertHrefUsed()
    {
        Browser.FindElement(By.Id("show-custom-href")).Click();
        var link = Browser.FindElement(By.Id("custom-href-download-link"));
        var href = link.GetAttribute("href");
        Assert.Equal("javascript:void(0)", href);
    }

    [Fact]
    public void RapidClicks_CancelsFirstAndStartsSecond()
    {
        // Instrument with controllable delay on first call for cancellation scenario
        var success = ((IJavaScriptExecutor)Browser).ExecuteAsyncScript(@"
            var callback = arguments[arguments.length - 1];
            (function(){
                function patch(){
                    const root = Blazor && Blazor._internal && Blazor._internal.BinaryMedia;
                    if (!root || !root.downloadAsync){ requestAnimationFrame(patch); return; }
                    if (!window.__origDownloadAsyncDelay){
                        window.__origDownloadAsyncDelay = root.downloadAsync;
                        window.__downloadCalls = 0;
                        window.__downloadDelayResolvers = null;
                        root.downloadAsync = async function(...a){
                            window.__downloadCalls++;
                            if (window.__downloadCalls === 1){
                                const getResolvers = () => {
                                    if (Promise.fromResolvers) return Promise.fromResolvers();
                                    let resolve, reject; const p = new Promise((r,j)=>{ resolve=r; reject=j; });
                                    return { promise: p, resolve, reject };
                                };
                                if (!window.__downloadDelayResolvers){
                                    window.__downloadDelayResolvers = getResolvers();
                                }
                                await window.__downloadDelayResolvers.promise;
                            }
                            return window.__origDownloadAsyncDelay.apply(this, a);
                        };
                    }
                    callback(true);
                }
                patch();
            })();
        ") is true;
        Assert.True(success, "Failed to instrument for rapid clicks test");

        Browser.FindElement(By.Id("show-download")).Click();
        var link = Browser.FindElement(By.Id("download-link"));
        link.Click(); // first (delayed)
        link.Click(); // second should cancel first

        ((IJavaScriptExecutor)Browser).ExecuteScript("if (window.__downloadDelayResolvers) { window.__downloadDelayResolvers.resolve(); }");

        Browser.True(() => Convert.ToInt32(((IJavaScriptExecutor)Browser).ExecuteScript("return window.__downloadCalls || 0;"), CultureInfo.InvariantCulture) >= 2);
        Browser.True(() => string.IsNullOrEmpty(link.GetAttribute("data-state")) || link.GetAttribute("data-state") == null);

        // Cleanup instrumentation
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            (function(){
              const root = Blazor && Blazor._internal && Blazor._internal.BinaryMedia;
              if (root && window.__origDownloadAsyncDelay){ root.downloadAsync = window.__origDownloadAsyncDelay; delete window.__origDownloadAsyncDelay; }
              delete window.__downloadDelayResolvers;
            })();");
    }

    [Fact]
    public void TemplatedFileDownload_Works()
    {
        InstrumentDownload();
        Browser.FindElement(By.Id("show-templated-download")).Click();
        var link = Browser.FindElement(By.Id("templated-download-link"));
        Assert.NotNull(link);
        link.Click();
        Browser.True(() => GetDownloadCallCount() >= 1);
        Browser.True(() => GetLastFileName() == "templated.png");
        var status = Browser.FindElement(By.Id("templated-download-status")).Text;
        Assert.True(status == "Idle/Done");
    }
}
