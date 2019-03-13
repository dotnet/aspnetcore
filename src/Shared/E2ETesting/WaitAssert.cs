// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Microsoft.AspNetCore.E2ETesting
{
    // XUnit assertions, but hooked into Selenium's polling mechanism

    public class WaitAssert
    {
        private readonly static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

        public static void Equal<T>(T expected, Func<T> actual)
            => WaitAssertCore(() => Assert.Equal(expected, actual()));

        public static void True(Func<bool> actual)
            => WaitAssertCore(() => Assert.True(actual()));

        public static void True(Func<bool> actual, TimeSpan timeout)
            => WaitAssertCore(() => Assert.True(actual()), timeout);

        public static void False(Func<bool> actual)
            => WaitAssertCore(() => Assert.False(actual()));

        public static void Contains(string expectedSubstring, Func<string> actualString)
            => WaitAssertCore(() => Assert.Contains(expectedSubstring, actualString()));

        public static void Collection<T>(Func<IEnumerable<T>> actualValues, params Action<T>[] elementInspectors)
            => WaitAssertCore(() => Assert.Collection(actualValues(), elementInspectors));

        public static void Empty(Func<IEnumerable> actualValues)
            => WaitAssertCore(() => Assert.Empty(actualValues()));

        public static void Single(Func<IEnumerable> actualValues)
            => WaitAssertCore(() => Assert.Single(actualValues()));

        private static void WaitAssertCore(Action assertion, TimeSpan timeout = default)
        {
            if (timeout == default)
            {
                timeout = DefaultTimeout;
            }

            try
            {
                new WebDriverWait(BrowserTestBase.Browser, timeout).Until(_ =>
                {
                    try
                    {
                        assertion();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                // Instead of reporting it as a timeout, report the Xunit exception
                assertion();
            }
        }
    }
}
