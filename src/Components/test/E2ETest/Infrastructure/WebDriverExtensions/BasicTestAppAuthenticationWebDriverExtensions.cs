// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETest;

internal static class BasicTestAppAuthenticationWebDriverExtensions
{
    public static void SignInAs(this IWebDriver browser, Uri baseUri, string usernameOrNull, string rolesOrNull, bool useSeparateTab = false)
    {
        var basePath = baseUri.LocalPath.EndsWith('/') ? baseUri.LocalPath : baseUri.LocalPath + "/";
        var authenticationPageUrl = $"{basePath}Authentication";
        var baseRelativeUri = usernameOrNull == null
            ? $"{authenticationPageUrl}?signout=true"
            : $"{authenticationPageUrl}?username={usernameOrNull}&roles={rolesOrNull}";

        if (useSeparateTab)
        {
            // Some tests need to change the authentication state without discarding the
            // original page, but this adds several seconds of delay
            var originalWindow = browser.CurrentWindowHandle;
            browser.SwitchTo().NewWindow(WindowType.Tab);
            browser.Navigate(baseUri, baseRelativeUri);
            browser.Exists(By.CssSelector("h1#authentication"));
            browser.Close();
            browser.SwitchTo().Window(originalWindow);
        }
        else
        {
            browser.Navigate(baseUri, baseRelativeUri);
            browser.Exists(By.CssSelector("h1#authentication"));
        }
    }
}
