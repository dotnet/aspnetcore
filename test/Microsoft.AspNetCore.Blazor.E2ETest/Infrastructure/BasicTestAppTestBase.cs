// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure
{
    public class BasicTestAppTestBase : ServerTestBase<DevHostServerFixture<Program>>
    {
        public const string ServerPathBase = "/subdir";

        public BasicTestAppTestBase(BrowserFixture browserFixture, DevHostServerFixture<Program> serverFixture)
            : base(browserFixture, serverFixture)
        {
            serverFixture.PathBase = ServerPathBase;
        }

        protected IWebElement MountTestComponent<TComponent>() where TComponent : IComponent
        {
            var componentTypeName = typeof(TComponent).FullName;
            WaitUntilDotNetRunningInBrowser();
            ((IJavaScriptExecutor)Browser).ExecuteScript(
                $"mountTestComponent('{componentTypeName}')");
            return Browser.FindElement(By.TagName("app"));
        }

        protected void WaitUntilDotNetRunningInBrowser()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(driver =>
            {
                return ((IJavaScriptExecutor)driver)
                    .ExecuteScript("return window.isTestReady;");
            });
        }
    }
}
