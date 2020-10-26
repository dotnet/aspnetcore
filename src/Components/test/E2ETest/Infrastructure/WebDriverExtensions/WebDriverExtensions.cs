using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Components.E2ETest
{
    internal static class WebDriverExtensions
    {
        public static void Navigate(this IWebDriver browser, Uri baseUri, string relativeUrl, bool noReload)
        {
            var absoluteUrl = new Uri(baseUri, relativeUrl);

            if (noReload)
            {
                var existingUrl = browser.Url;
                if (string.Equals(existingUrl, absoluteUrl.AbsoluteUri, StringComparison.Ordinal))
                {
                    return;
                }
            }

            browser.Navigate().GoToUrl("about:blank");
            browser.Navigate().GoToUrl(absoluteUrl);
        }
    }
}
