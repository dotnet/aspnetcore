// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure
{
    public class BasicTestAppTestBase : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public string ServerPathBase
            => "/subdir" + (_serverFixture.ExecutionMode == ExecutionMode.Server ? "#server" : "");

        public BasicTestAppTestBase(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            serverFixture.PathBase = ServerPathBase;
        }

        protected IWebElement MountTestComponent<TComponent>() where TComponent : IComponent
        {
            var componentTypeName = typeof(TComponent).FullName;
            var testSelector = WaitUntilTestSelectorReady();
            testSelector.SelectByValue("none");
            testSelector.SelectByValue(componentTypeName);
            return Browser.FindElement(By.TagName("app"));
        }

        protected SelectElement WaitUntilTestSelectorReady()
        {
            var elemToFind = By.CssSelector("#test-selector > select");
            WaitUntilExists(elemToFind, timeoutSeconds: 30, throwOnError: true);
            return new SelectElement(Browser.FindElement(elemToFind));
        }

        protected void SignInAs(string usernameOrNull, string rolesOrNull, bool useSeparateTab = false)
        {
            const string authenticationPageUrl = "/Authentication";
            var baseRelativeUri = usernameOrNull == null
                ? $"{authenticationPageUrl}?signout=true"
                : $"{authenticationPageUrl}?username={usernameOrNull}&roles={rolesOrNull}";

            if (useSeparateTab)
            {
                // Some tests need to change the authentication state without discarding the
                // original page, but this adds several seconds of delay
                var javascript = (IJavaScriptExecutor)Browser;
                var originalWindow = Browser.CurrentWindowHandle;
                javascript.ExecuteScript("window.open()");
                Browser.SwitchTo().Window(Browser.WindowHandles.Last());
                Navigate(baseRelativeUri);
                WaitUntilExists(By.CssSelector("h1#authentication"));
                javascript.ExecuteScript("window.close()");
                Browser.SwitchTo().Window(originalWindow);
            }
            else
            {
                Navigate(baseRelativeUri);
                WaitUntilExists(By.CssSelector("h1#authentication"));
            }
        }
    }
}
