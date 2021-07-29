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
            Assert.Equal(new string('c', 25_000), valueInTextArea);

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

            var newValue = new string('a', 25_000);
            SetTextAreaValueInBrowser(newValue);

            getTextBtn.Click();
            Browser.Equal(newValue, () => textResultFromComponent.GetAttribute("innerHTML"));

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
            var newValue = new string('f', 25_000);
            SetTextAreaValueInBrowser(newValue);

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

            var firstTick = Convert.ToInt64(lastChangedTime.GetAttribute("value"));
            Assert.True(firstTick > 0);
            var firstLength = Convert.ToInt32(lastChangedLength.GetAttribute("value"));
            Assert.Equal(3, firstLength);

            // Ensure time passes between first and second changes
            await Task.Delay(1000);

            textArea.SendKeys("123");
            FocusAway();

            var secondTick = Convert.ToInt64(lastChangedTime.GetAttribute("value"));
            Assert.True(secondTick > firstTick);
            var secondLengthLength = Convert.ToInt32(lastChangedLength.GetAttribute("value"));
            Assert.Equal(6, secondLengthLength);

            FocusAway();
            AssertLogDoesNotContainMessages(CircuitErrors);
        }

        [Fact]
        public void CanEditValue_LargeAmountOfContent_Delete()
        {
            var newValue = new string('f', 25_000);
            SetTextAreaValueInBrowser(newValue);

            var textArea = Browser.Exists(By.Id("largeTextArea"), TimeSpan.FromSeconds(10));
            Assert.NotNull(textArea);

            for (var i = 0; i < 500; i++)
            {
                textArea.SendKeys(Keys.Delete);
            }

            Assert.Equal(new string('f', 24_500), GetTextAreaValueFromBrowser());

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

        private void SetTextAreaValueInBrowser(string newValue)
        {
            var javascript = (IJavaScriptExecutor)Browser;
            javascript.ExecuteScript($"document.getElementById(\"largeTextArea\").value = {newValue};");
        }

        private void FocusAway()
        {
            var focusAwayBtn = Browser.Exists(By.Id("focusAwayBtn"));
            focusAwayBtn.Click();
        }
    }
}
