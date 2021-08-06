// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class InputLargeTextAreaTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public InputLargeTextAreaTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
            Browser.MountTestComponent<InputLargeTextAreaComponent>();
        }

        // public override async Task InitializeAsync()
        // {
        //     // Since the tests share interactivity with the same text area, it's easiest for each
        //     // test to run in its own browser instance.
        //     await base.InitializeAsync(Guid.NewGuid().ToString());
        // }

        [Fact]
        public void CanSetValue()
        {
            // Set large value
            var setTextBtn = Browser.Exists(By.Id("setTextBtn"));
            setTextBtn.Click();

            var valueInTextArea = GetTextAreaValueFromBrowser();
            Assert.Equal(new string('c', 50_000), valueInTextArea);

            AssertInteractivityIsMaintained();
        }

        [Fact]
        public void CanGetValue()
        {
            var getTextBtn = Browser.Exists(By.Id("getTextBtn"));
            getTextBtn.Click();

            var textResultFromComponent = Browser.Exists(By.Id("getTextResult"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textResultFromComponent);
            Browser.Equal(string.Empty, () => textResultFromComponent.GetAttribute("innerHTML"));

            var newValue = new string('a', 50_000);
            SetTextAreaValueInBrowser('a');

            getTextBtn.Click();
            Browser.Equal(newValue, () => textResultFromComponent.GetAttribute("innerHTML"));

            AssertInteractivityIsMaintained();
        }

        [Fact]
        public void CanGetValue_ThrowsIfTextAreaHasMoreContentThanMaxAllowed()
        {
            // Prime the textarea with 50k chars (more than 32k default limit)
            SetTextAreaValueInBrowser('a');

            var getLimitedTextBtn = Browser.Exists(By.Id("getLimitedTextBtn"));
            getLimitedTextBtn.Click();

            var textErrorFromComponent = Browser.Exists(By.Id("getTextError"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textErrorFromComponent);

            var expectedError = "The incoming data stream of length 50000 exceeds the maximum allowed length 32000.";
            Browser.Contains(expectedError, () => textErrorFromComponent.GetAttribute("innerHTML"));

            AssertInteractivityIsMaintained();
        }

        [Fact]
        public void CanEditValue_MinimalContent()
        {
            var textArea = Browser.Exists(By.Id("largeTextArea"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textArea);
            textArea.SendKeys("abc");

            Assert.Equal("abc", GetTextAreaValueFromBrowser());

            AssertInteractivityIsMaintained();
        }

        [Fact]
        public void CanEditValue_LargeAmountOfContent_Insert()
        {
            var newValue = new string('f', 50_000);
            SetTextAreaValueInBrowser('f');

            var textArea = Browser.Exists(By.Id("largeTextArea"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textArea);
            textArea.SendKeys("abc");

            Assert.Equal(newValue + "abc", GetTextAreaValueFromBrowser());

            AssertInteractivityIsMaintained();
        }

        [Fact]
        public void OnChangeTriggers()
        {
            var lastChangedLength = Browser.Exists(By.Id("lastChangedLength"));
            Assert.NotNull(lastChangedLength);

            var textArea = Browser.Exists(By.Id("largeTextArea"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textArea);
            textArea.SendKeys("abc");
            FocusAway();

            var firstLength = Convert.ToInt32(lastChangedLength.GetAttribute("innerHTML"));
            Assert.Equal(3, firstLength);

            textArea.SendKeys("123");
            FocusAway();

            var secondLengthLength = Convert.ToInt32(lastChangedLength.GetAttribute("innerHTML"));
            Assert.Equal(6, secondLengthLength);

            AssertInteractivityIsMaintained();
        }

        [Fact]
        public void CanEditValue_LargeAmountOfContent_Delete()
        {
            SetTextAreaValueInBrowser('g');

            var textArea = Browser.Exists(By.Id("largeTextArea"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textArea);

            for (var i = 0; i < 500; i++)
            {
                textArea.SendKeys(Keys.Backspace);
            }

            Assert.Equal(new string('g', 49_500), GetTextAreaValueFromBrowser());

            AssertInteractivityIsMaintained();
        }

        private string GetTextAreaValueFromBrowser()
        {
            var textArea = Browser.Exists(By.Id("largeTextArea"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textArea);
            return (string)textArea.GetAttribute("value");
        }

        private void SetTextAreaValueInBrowser(char charToRepeat, int numChars = 50_000)
        {
            var javascript = (IJavaScriptExecutor)Browser;
            javascript.ExecuteScript($"document.getElementById(\"largeTextArea\").value = '{charToRepeat}'.repeat({numChars});");
        }

        private void FocusAway()
        {
            var focusAwayBtn = Browser.Exists(By.Id("focusAwayBtn"));
            Assert.NotNull(focusAwayBtn);
            focusAwayBtn.Click();
        }

        private void AssertInteractivityIsMaintained()
        {
            var interactivityCounter = Browser.Exists(By.Id("interactivityCounter"));
            Assert.NotNull(interactivityCounter);
            var initialCount = Convert.ToInt32(interactivityCounter.GetAttribute("innerHTML"));

            var incrementInteractivityCounterBtn = Browser.Exists(By.Id("incrementInteractivityCounterBtn"));
            Assert.NotNull(incrementInteractivityCounterBtn);
            incrementInteractivityCounterBtn.Click();

            var incrementedCount = Convert.ToInt32(interactivityCounter.GetAttribute("innerHTML"));

            Assert.Equal(initialCount + 1, incrementedCount);
        }
    }
}
