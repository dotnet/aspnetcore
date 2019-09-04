// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Microsoft.AspNetCore.E2ETesting
{
    // XUnit assertions, but hooked into Selenium's polling mechanism

    public static class WaitAssert
    {
        public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(E2ETestOptions.Instance.DefaultWaitTimeoutInSeconds);

        public static void Equal<T>(this IWebDriver driver, T expected, Func<T> actual)
            => WaitAssertCore(driver, () => Assert.Equal(expected, actual()));

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

        public static void Exists(this IWebDriver driver, By finder)
            => Exists(driver, finder, default);

        public static void Exists(this IWebDriver driver, By finder, TimeSpan timeout)
            => WaitAssertCore(driver, () => Assert.NotEmpty(driver.FindElements(finder)), timeout);

        private static void WaitAssertCore(IWebDriver driver, Action assertion, TimeSpan timeout = default)
        {
            if (timeout == default)
            {
                timeout = DefaultTimeout;
            }

            Exception lastException = null;
            try
            {
                new WebDriverWait(driver, timeout).Until(_ =>
                {
                    try
                    {
                        assertion();
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
                var fileId = $"{Guid.NewGuid()}.png";
                var screenShotPath = Path.Combine(Path.GetFullPath(E2ETestOptions.Instance.ScreenShotsPath), fileId);
                var errors = driver.GetBrowserLogs(LogLevel.Severe);

                if (driver is ITakesScreenshot takesScreenshot && E2ETestOptions.Instance.ScreenShotsPath != null)
                {
                    try
                    {
                        if (!Directory.Exists(E2ETestOptions.Instance.ScreenShotsPath))
                        {
                            Directory.CreateDirectory(E2ETestOptions.Instance.ScreenShotsPath);
                        }

                        var screenShot = takesScreenshot.GetScreenshot();
                        screenShot.SaveAsFile(screenShotPath);
                    }
                    catch (Exception)
                    {
                    }
                }
                var exceptionInfo = lastException != null ? ExceptionDispatchInfo.Capture(lastException) :
                    CaptureException(assertion);

                throw new BrowserAssertFailedException(errors, exceptionInfo.SourceException, screenShotPath);

                static ExceptionDispatchInfo CaptureException(Action assertion)
                {
                    try
                    {
                        assertion();
                        throw new InvalidOperationException("The assertion succeded after the timeout.");
                    }
                    catch (Exception ex)
                    {
                        return ExceptionDispatchInfo.Capture(ex);
                    }
                }
            }
        }
    }
}
