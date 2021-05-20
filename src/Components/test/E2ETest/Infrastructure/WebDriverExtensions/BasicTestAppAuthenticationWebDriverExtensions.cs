using System;
using System.Linq;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETest
{
    internal static class BasicTestAppAuthenticationWebDriverExtensions
    {
        public static void SignInAs(this IWebDriver browser, Uri baseUri, string usernameOrNull, string rolesOrNull, bool useSeparateTab = false)
        {
            var basePath = baseUri.LocalPath.EndsWith("/", StringComparison.Ordinal) ? baseUri.LocalPath : baseUri.LocalPath + "/";
            var authenticationPageUrl = $"{basePath}Authentication";
            var baseRelativeUri = usernameOrNull == null
                ? $"{authenticationPageUrl}?signout=true"
                : $"{authenticationPageUrl}?username={usernameOrNull}&roles={rolesOrNull}";

            if (useSeparateTab)
            {
                // Some tests need to change the authentication state without discarding the
                // original page, but this adds several seconds of delay
                var javascript = (IJavaScriptExecutor)browser;
                var originalWindow = browser.CurrentWindowHandle;
                javascript.ExecuteScript("window.open()");
                browser.SwitchTo().Window(browser.WindowHandles.Last());
                browser.Navigate(baseUri, baseRelativeUri, noReload: false);
                browser.Exists(By.CssSelector("h1#authentication"));
                javascript.ExecuteScript("window.close()");
                browser.SwitchTo().Window(originalWindow);
            }
            else
            {
                browser.Navigate(baseUri, baseRelativeUri, noReload: false);
                browser.Exists(By.CssSelector("h1#authentication"));
            }
        }
    }
}
