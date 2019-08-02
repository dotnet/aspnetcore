// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Microsoft.AspNetCore.E2ETesting
{
    // XUnit assertions, but hooked into Selenium's polling mechanism

    public static class WaitAssert
    {
        public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

        public static void Equal<T>(this IWebDriver driver, T expected, Func<T> actual)
            => WaitAssertCore(driver, () => Assert.Equal(expected, actual()));

        public static void True(this IWebDriver driver, Func<bool> actual)
            => WaitAssertCore(driver, () => Assert.True(actual()));

        public static void ExecuteScript(this IWebDriver driver, string script)
        {
            if (!(driver is IJavaScriptExecutor javaScript))
            {
                Assert.False(true, "The driver can't execute JavaScript.");
                return;
            }
            else
            {
                javaScript.ExecuteScript(script);
            }
        }

        public static void ExecuteAsyncScript(this IWebDriver driver, string script)
        {
            if (!(driver is IJavaScriptExecutor javaScript))
            {
                Assert.False(true, "The driver can't execute JavaScript.");
                return;
            }
            else
            {
                var scriptWithCallback = $"const cb = arguments[arguments.length - 1];{script}.then(cb, cb)";
                javaScript.ExecuteAsyncScript(scriptWithCallback);
            }
        }
        public static void HasJavaScriptValue<T>(
            this IWebDriver driver,
            T expectedValue,
            string script,
            Func<object, T> converter = null)
        {
            if(!script.StartsWith("return "))
            {
                script = $"return {script}";
            }

            converter ??= (v) => (T)v;
            if (!(driver is IJavaScriptExecutor javaScript))
            {
                Assert.False(true, "The driver can't execute JavaScript.");
            }
            else
            {
                driver.True(() =>
                {
                    var result = javaScript.ExecuteScript(script);
                    T convertedResult = converter(result);
                    return expectedValue.Equals(convertedResult);
                });
            }
        }

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
            => WaitAssertCore(driver, () => Assert.NotEmpty(driver.FindElements(finder)));

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
                if (lastException != null)
                {
                    ExceptionDispatchInfo.Capture(lastException).Throw();
                }
                else
                {
                    // Instead of reporting it as a timeout, report the Xunit exception
                    assertion();
                }
            }
        }
    }
}
