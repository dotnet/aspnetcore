// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class FormsTest : BasicTestAppTestBase
    {
        public FormsTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, noReload: !serverFixture.UsingAspNetHost);
        }

        [Fact]
        public async Task EditFormWorksWithDataAnnotationsValidator()
        {
            var appElement = MountTestComponent<SimpleValidationComponent>();
            var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
            var acceptsTermsInput = appElement.FindElement(By.ClassName("accepts-terms")).FindElement(By.TagName("input"));
            var submitButton = appElement.FindElement(By.TagName("button"));

            // Editing a field doesn't trigger validation on its own
            userNameInput.SendKeys("Bert\t");
            acceptsTermsInput.Click(); // Accept terms
            acceptsTermsInput.Click(); // Un-accept terms
            await Task.Delay(500); // There's no expected change to the UI, so just wait a moment before asserting
            Assert.Empty(appElement.FindElements(By.ClassName("validation-message")));
            Assert.Empty(appElement.FindElements(By.Id("last-callback")));

            // Submitting the form does validate
            submitButton.Click();
            WaitAssert.Collection(() => appElement.FindElements(By.ClassName("validation-message")),
                li => Assert.Equal("You must accept the terms", li.Text));
            WaitAssert.Equal("OnInvalidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);

            // Can make another field invalid
            userNameInput.Clear();
            submitButton.Click();
            WaitAssert.Collection(() => appElement.FindElements(By.ClassName("validation-message")).OrderBy(x => x.Text),
                li => Assert.Equal("Please choose a username", li.Text),
                li => Assert.Equal("You must accept the terms", li.Text));
            WaitAssert.Equal("OnInvalidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);

            // Can make valid
            userNameInput.SendKeys("Bert\t");
            acceptsTermsInput.Click();
            submitButton.Click();
            WaitAssert.Empty(() => appElement.FindElements(By.ClassName("validation-message")));
            WaitAssert.Equal("OnValidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);
        }
    }
}
