// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Microsoft.AspNetCore.E2ETesting;

// XUnit assertions, but hooked into Selenium's polling mechanism

public static class WaitAssert
{
    private static bool TestRunFailed;
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

    private static TResult WaitAssertCore<TResult>(IWebDriver driver, Func<TResult> assertion, TimeSpan timeout = default)
    {
        if (timeout == default)
        {
            timeout = !TestRunFailed ? DefaultTimeout : FailureTimeout;
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
            // At this point at least one test failed, so we mark the test as failed. Any assertions after this one
            // will fail faster. There's a small race condition here between checking the value for TestRunFailed
            // above and setting it here, but nothing bad can come out of it. Worst case scenario, one or more
            // tests running concurrently might use the DefaultTimeout in their current assertion, which is fine.
            TestRunFailed = true;

            var innerHtml = driver.FindElement(By.CssSelector(":first-child"))?.GetDomProperty("innerHTML");

            var fileId = $"{Guid.NewGuid():N}.png";
            var screenShotPath = Path.Combine(Path.GetFullPath(E2ETestOptions.Instance.ScreenShotsPath), fileId);
            var errors = driver.GetBrowserLogs(LogLevel.All).Select(c => c.ToString()).ToList();

            TakeScreenShot(driver, screenShotPath);
            var exceptionInfo = lastException != null ? ExceptionDispatchInfo.Capture(lastException) :
                CaptureException(() => assertion());

            throw new BrowserAssertFailedException(errors, exceptionInfo.SourceException, screenShotPath, innerHtml);
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
}
