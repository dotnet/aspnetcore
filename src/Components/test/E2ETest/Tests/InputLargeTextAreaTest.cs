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
        private static readonly string[] CircuitErrors = new[]
        {
            "Connection disconnected with error 'Error: Server returned an error on close: Connection closed with an error.'.",
            "Cannot send data if the connection is not in the 'Connected' State.",
            "HubConnection.connectionClosed(Error: Server returned an error on close: Connection closed with an error.) called while in state Connected.",
        };

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

        [Fact]
        public void CanSetValue()
        {
            // Set large value
            var setTextBtn = Browser.Exists(By.Id("setTextBtn"));
            setTextBtn.Click();

            var valueInTextArea = GetTextAreaValueFromBrowser();
            Assert.Equal(new string('c', 50_000), valueInTextArea);

            FocusAway();
            AssertLogDoesNotContainMessages(CircuitErrors);
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

            FocusAway();
            AssertLogDoesNotContainMessages(CircuitErrors);
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

            var expectedError = "The incoming data stream of length 50000 exceeds the maximum allowed length 32000. (Parameter 'maxAllowedSize')";
            Browser.Contains(expectedError, () => textErrorFromComponent.GetAttribute("innerHTML"));

            FocusAway();
            AssertLogDoesNotContainMessages(CircuitErrors);
        }

        [Fact]
        public void CanEditValue_MinimalContent()
        {
            var textArea = Browser.Exists(By.Id("largeTextArea"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textArea);
            textArea.SendKeys("abc");

            Assert.Equal("abc", GetTextAreaValueFromBrowser());

            FocusAway();
            AssertLogDoesNotContainMessages(CircuitErrors);
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

            FocusAway();
            AssertLogDoesNotContainMessages(CircuitErrors);
        }

        [Fact]
        public async Task OnChangeTriggers()
        {
            var lastChangedTime = Browser.Exists(By.Id("lastChangedTime"));
            Assert.NotNull(lastChangedTime);

            var lastChangedLength = Browser.Exists(By.Id("lastChangedLength"));
            Assert.NotNull(lastChangedLength);

            var textArea = Browser.Exists(By.Id("largeTextArea"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textArea);
            textArea.SendKeys("abc");
            FocusAway();

            var firstTick = Convert.ToInt64(lastChangedTime.GetAttribute("innerHTML"));
            Assert.True(firstTick > 0);
            var firstLength = Convert.ToInt32(lastChangedLength.GetAttribute("innerHTML"));
            Assert.Equal(3, firstLength);

            // Ensure time passes between first and second changes
            await Task.Delay(1000);

            textArea.SendKeys("123");
            FocusAway();

            var secondTick = Convert.ToInt64(lastChangedTime.GetAttribute("innerHTML"));
            Assert.True(secondTick > firstTick);
            var secondLengthLength = Convert.ToInt32(lastChangedLength.GetAttribute("innerHTML"));
            Assert.Equal(6, secondLengthLength);

            FocusAway();
            AssertLogDoesNotContainMessages(CircuitErrors);
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

            FocusAway();
            AssertLogDoesNotContainMessages(CircuitErrors);
        }

        private void AssertLogDoesNotContainMessages(params string[] messages)
        {
            var log = Browser.Manage().Logs.GetLog(LogType.Browser);
            foreach (var message in messages)
            {
                Assert.DoesNotContain(log, entry => entry.Message.Contains(message));
            }
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
            focusAwayBtn.Click();
        }
    }
}
