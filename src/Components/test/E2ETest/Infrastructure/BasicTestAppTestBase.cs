// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure
{
    public class BasicTestAppTestBase : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public string ServerPathBase
            => "/subdir" + (_serverFixture.UsingAspNetHost ? "#server" : "");

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

            // Wait until we've torn down any earlier component
            testSelector.SelectByValue("none");
            WaitUntilExists(By.Id("no-test-selected"), timeoutSeconds: 30);

            // Wait until we've done the initial render for the new component
            testSelector.SelectByValue(componentTypeName);
            return WaitUntilExists(By.TagName("app"), timeoutSeconds: 30);
        }

        protected SelectElement WaitUntilTestSelectorReady()
        {
            var elemToFind = By.CssSelector("#test-selector > select");
            WaitUntilExists(elemToFind, timeoutSeconds: 30);
            return new SelectElement(Browser.FindElement(elemToFind));
        }

        protected IWebElement WaitUntilExists(By findBy, int timeoutSeconds = 10)
        {
            IWebElement result = null;
            new WebDriverWait(Browser, TimeSpan.FromSeconds(timeoutSeconds))
                .Until(driver => (result = driver.FindElement(findBy)) != null);
            return result;
        }
    }
}
