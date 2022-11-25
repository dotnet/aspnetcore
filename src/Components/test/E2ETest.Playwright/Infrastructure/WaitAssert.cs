// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.E2ETesting;

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
        try
        {
            var endTime = DateTime.Now.Add(timeout);
            while (true)
            {
                if (DateTime.Now > endTime)
                {
                    throw new WebDriverTimeoutException(lastException);
                }

                try
                {
                    return assertion();
                }
                catch (Exception e)
                {
                    lastException = e;
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }
            }
        }
        catch (WebDriverTimeoutException)
        {
            // At this point at least one test failed, so we mark the test as failed. Any assertions after this one
            // will fail faster. There's a small race condition here between checking the value for TestRunFailed
            // above and setting it here, but nothing bad can come out of it. Worst case scenario, one or more
            // tests running concurrently might use the DefaultTimeout in their current assertion, which is fine.
            TestRunFailed = true;
            throw;
        }
    }

    private class WebDriverTimeoutException : Exception
    {
        public WebDriverTimeoutException(Exception lastException)
            : base("Assertion timed out", lastException)
        {
        }
    }
}
