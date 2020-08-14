// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class InputActionsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public InputActionsTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        }

        protected virtual IWebElement MountInputActionsComponent()
            => Browser.MountTestComponent<InputActionsComponent>();

        [Theory]
        [InlineData("name")]
        [InlineData("age")]
        [InlineData("description")]
        [InlineData("renewal-date")]
        [InlineData("radio-group")]
        [InlineData("accepts-terms")]
        [InlineData("ticket-class")]
        public void InputElementsGetFocusedSuccessfully(string className)
        {
            var appElement = MountInputActionsComponent();
            var inputSection = appElement.FindElement(By.ClassName(className));
            var buttonsToFocus = inputSection.FindElements(By.TagName("button"));
            var inputsToFocus = inputSection.FindElements(By.TagName("input"));

            if (inputsToFocus.Count == 0)
            {
                inputsToFocus = inputSection.FindElements(By.TagName("textarea"));
            }

            if (inputsToFocus.Count == 0)
            {
                inputsToFocus = inputSection.FindElements(By.TagName("select"));
            }

            for (int i = 0; i < buttonsToFocus.Count; i++)
            {
                buttonsToFocus[i].Click();
                Browser.Equal(inputsToFocus[i], () => Browser.SwitchTo().ActiveElement());
            }
        }
    }
}
