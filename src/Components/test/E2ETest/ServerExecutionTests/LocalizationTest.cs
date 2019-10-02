// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    // For now this is limited to server-side execution because we don't have the ability to set the
    // culture in client-side Blazor.
    public class LocalizationTest : BasicTestAppTestBase
    {
        public LocalizationTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, _serverFixture.ExecutionMode == ExecutionMode.Client);
            MountTestComponent<CulturePicker>();
            WaitUntilExists(By.Id("culture-selector"));
        }

        [Theory]
        [InlineData("en-US", "Hello!")]
        [InlineData("fr-FR", "Bonjour!")]
        public void CanSetCultureAndReadLocalizedResources(string culture, string message)
        {
            var selector = new SelectElement(Browser.FindElement(By.Id("culture-selector")));
            selector.SelectByValue(culture);

            // Click the link to return back to the test page
            WaitUntilExists(By.ClassName("return-from-culture-setter")).Click();

            // That should have triggered a page load, so wait for the main test selector to come up.
            MountTestComponent<LocalizedText>();

            var cultureDisplay = WaitUntilExists(By.Id("culture-name-display"));
            Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);

            var messageDisplay = Browser.FindElement(By.Id("message-display"));
            Assert.Equal(message, messageDisplay.Text);
        }
    }
}
