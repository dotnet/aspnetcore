// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Microsoft.AspNetCore.E2ETesting;

// XUnit assertions, but hooked into Selenium's polling mechanism

public static class WaitAssert
{
    private static int _failureCount;
    public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(E2ETestOptions.Instance.DefaultWaitTimeoutInSeconds);
    public static TimeSpan FailureTimeout = TimeSpan.FromSeconds(E2ETestOptions.Instance.DefaultAfterFailureWaitTimeoutInSeconds);

    public static void Equal<T>(this IWebDriver driver, T expected, Func<T> actual)
        => WaitAssertCore(driver, () => Assert.Equal(expected, actual()));

    public static void NotEqual<T>(this IWebDriver driver, T expected, Func<T> actual)
        => WaitAssertCore(driver, () => Assert.NotEqual(expected, actual()));

    public static void True(this IWebDriver driver, Func<bool> actual)
        => WaitAssertCore(driver, () => Assert.True(actual()));

    public static void True(this IWebDriver driver, Func<bool> actual, TimeSpan timeout)
        => WaitAssertCore(driver, () => Assert.True(actual()), timeout);

    public static void True(this IWebDriver driver, Func<bool> actual, TimeSpan timeout, string message)
        => WaitAssertCore(driver, () => Assert.True(actual(), message), timeout);
    public static void True(this IWebDriver driver, Func<bool> actual, string message)
        => WaitAssertCore(driver, () => Assert.True(actual(), message));

    public static void False(this IWebDriver driver, Func<bool> actual, string message)
        => WaitAssertCore(driver, () => Assert.False(actual(), message));

    public static void False(this IWebDriver driver, Func<bool> actual)
        => WaitAssertCore(driver, () => Assert.False(actual()));

    public static void Contains(this IWebDriver driver, string expectedSubstring, Func<string> actualString)
        => WaitAssertCore(driver, () => Assert.Contains(expectedSubstring, actualString()));

    public static void Collection<T>(this IWebDriver driver, Func<IEnumerable<T>> actualValues, params Action<T>[] elementInspectors)
        => WaitAssertCore(driver, () => Assert.Collection(actualValues(), elementInspectors));

    public static void Empty(this IWebDriver driver, Func<IEnumerable> actualValues)
        => WaitAssertCore(driver, () => Assert.Empty(actualValues()));

    public static void Single(this IWebDriver driver, Func<IEnumerable> actualValues)
        => WaitAssertCore(driver, () => Assert.Single(actualValues()));

    public static IWebElement Exists(this IWebDriver driver, By finder)
        => Exists(driver, finder, default);

    public static TElement Exists<TElement>(this IWebDriver driver, Func<TElement> actual, TimeSpan timeout)
        => WaitAssertCore(driver, actual, timeout);

    public static void DoesNotExist(this IWebDriver driver, By finder, TimeSpan timeout = default)
        => WaitAssertCore(driver, () =>
        {
            var elements = driver.FindElements(finder);
            Assert.Empty(elements);
        }, timeout);

    public static IWebElement Exists(this IWebDriver driver, By finder, TimeSpan timeout)
        => WaitAssertCore(driver, () =>
        {
            var elements = driver.FindElements(finder);
            Assert.NotEmpty(elements);
            var result = elements[0];
            return result;
        }, timeout);

    public static void Click(this IWebDriver driver, By selector)
        => WaitAssertCore(driver, () =>
        {
            driver.FindElement(selector).Click();
        });

    private static void WaitAssertCore(IWebDriver driver, Action assertion, TimeSpan timeout = default)
    {
        WaitAssertCore<object>(driver, () => { assertion(); return null; }, timeout);
    }

    // Number of failures before the timeout is halved.
    private const int FailuresPerStep = 5;

    private static TimeSpan GetAdjustedTimeout()
    {
        var failures = Volatile.Read(ref _failureCount);
        if (failures <= 0)
        {
            return DefaultTimeout;
        }

        // Halve the timeout every FailuresPerStep failures:
        // 0-4 failures: 120s, 5-9: 60s, 10-14: 30s, 15-19: 15s, 20-24: 7.5s, 25+: floor
        var steps = failures / FailuresPerStep;
        var seconds = DefaultTimeout.TotalSeconds / Math.Pow(2, steps);
        return TimeSpan.FromSeconds(Math.Max(seconds, FailureTimeout.TotalSeconds));
    }

    private static TResult WaitAssertCore<TResult>(IWebDriver driver, Func<TResult> assertion, TimeSpan timeout = default)
    {
        if (timeout == default)
        {
            timeout = GetAdjustedTimeout();
        }

        Exception lastException = null;
        TResult result = default;
        try
        {
            new WebDriverWait(driver, timeout).Until(_ =>
            {
                try
                {
                    result = assertion();
                    return true;
                }
                catch (Exception e)
                {
                    lastException = e;
                    return false;
                }
            });
        }
        catch (WebDriverTimeoutException)
        {
            // Increment the failure count so subsequent assertions use progressively shorter timeouts.
            // The timeout halves with each failure until it reaches the FailureTimeout floor.
            Interlocked.Increment(ref _failureCount);

            var currentUrl = string.Empty;
            try { currentUrl = driver.Url; }
            catch { /* Browser may be in a bad state */ }

            var innerHtml = driver.FindElement(By.CssSelector(":first-child"))?.GetDomProperty("innerHTML");

            var fileId = $"{Guid.NewGuid():N}.png";
            var screenShotPath = Path.Combine(Path.GetFullPath(E2ETestOptions.Instance.ScreenShotsPath), fileId);
            var errors = driver.GetBrowserLogs(LogLevel.All).Select(c => c.ToString()).ToList();
            var networkDetails = GetNetworkResponseDetails(driver);

            TakeScreenShot(driver, screenShotPath);
            var exceptionInfo = lastException != null ? ExceptionDispatchInfo.Capture(lastException) :
                CaptureException(() => assertion());

            throw new BrowserAssertFailedException(errors, exceptionInfo.SourceException, screenShotPath, innerHtml, currentUrl, networkDetails);
        }

        return result;
    }

    private static ExceptionDispatchInfo CaptureException(Action assertion)
    {
        try
        {
            assertion();
            throw new InvalidOperationException("The assertion succeeded after the timeout.");
        }
        catch (Exception ex)
        {
            return ExceptionDispatchInfo.Capture(ex);
        }
    }

    private static void TakeScreenShot(IWebDriver driver, string screenShotPath)
    {
        if (driver is ITakesScreenshot takesScreenshot && E2ETestOptions.Instance.ScreenShotsPath != null)
        {
            try
            {
                Directory.CreateDirectory(E2ETestOptions.Instance.ScreenShotsPath);

                var screenShot = takesScreenshot.GetScreenshot();
                screenShot.SaveAsFile(screenShotPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to take a screenshot {ex.ToString()}");
            }
        }
    }

    private static List<string> GetNetworkResponseDetails(IWebDriver driver)
    {
        var details = new List<string>();
        try
        {
            var performanceLogs = driver.Manage().Logs.GetLog("performance");
            foreach (var entry in performanceLogs)
            {
                if (!entry.Message.Contains("Network.responseReceived"))
                {
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(entry.Message);
                    var message = doc.RootElement.GetProperty("message");
                    var parameters = message.GetProperty("params");
                    var response = parameters.GetProperty("response");
                    var url = response.GetProperty("url").GetString();
                    var status = response.GetProperty("status").GetInt32();
                    var mimeType = response.TryGetProperty("mimeType", out var mt) ? mt.GetString() : "unknown";

                    // Only include framework-related URLs and error responses to keep output manageable
                    if (url is not null && (url.Contains("_framework") || url.Contains("_blazor") || status >= 400))
                    {
                        details.Add($"  [{status}] {mimeType} - {url}");
                    }
                }
                catch
                {
                    // Skip entries we can't parse
                }
            }
        }
        catch
        {
            // Performance logs might not be available
        }

        return details;
    }
}
