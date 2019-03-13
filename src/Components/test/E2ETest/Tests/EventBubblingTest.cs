// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class EventBubblingTest : BasicTestAppTestBase
    {
        // Note that currently we only support custom events if they have bubble:true.
        // That's because the event delegator doesn't know which custom events bubble and which don't,
        // so it doesn't know whether to register a normal handler or a capturing one. If this becomes
        // a problem, we could consider registering both types of handler and just bailing out from
        // the one that doesn't match the 'bubbles' flag on the received event object.

        public EventBubblingTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            Navigate(ServerPathBase, noReload: !serverFixture.UsingAspNetHost);
            MountTestComponent<EventBubblingComponent>();
        }
        
        [Fact]
        public void BubblingStandardEvent_FiredOnElementWithHandler()
        {
            Browser.FindElement(By.Id("button-with-onclick")).Click();

            // Triggers event on target and ancestors with handler in upwards direction
            WaitAssert.Equal(
                new[] { "target onclick", "parent onclick" },
                GetLogLines);
        }

        [Fact]
        public void BubblingStandardEvent_FiredOnElementWithoutHandler()
        {
            Browser.FindElement(By.Id("button-without-onclick")).Click();

            // Triggers event on ancestors with handler in upwards direction
            WaitAssert.Equal(
                new[] { "parent onclick" },
                GetLogLines);
        }

        [Fact]
        public void BubblingCustomEvent_FiredOnElementWithHandler()
        {
            TriggerCustomBubblingEvent("element-with-onsneeze", "sneeze");

            // Triggers event on target and ancestors with handler in upwards direction
            WaitAssert.Equal(
                new[] { "target onsneeze", "parent onsneeze" },
                GetLogLines);
        }

        [Fact]
        public void BubblingCustomEvent_FiredOnElementWithoutHandler()
        {
            TriggerCustomBubblingEvent("element-without-onsneeze", "sneeze");

            // Triggers event on ancestors with handler in upwards direction
            WaitAssert.Equal(
                new[] { "parent onsneeze" },
                GetLogLines);
        }

        [Fact]
        public void NonBubblingEvent_FiredOnElementWithHandler()
        {
            Browser.FindElement(By.Id("input-with-onfocus")).Click();

            // Triggers event only on target, not other ancestors with event handler
            WaitAssert.Equal(
                new[] { "target onfocus" },
                GetLogLines);
        }

        [Fact]
        public void NonBubblingEvent_FiredOnElementWithoutHandler()
        {
            Browser.FindElement(By.Id("input-without-onfocus")).Click();

            // Triggers no event
            WaitAssert.Empty(GetLogLines);
        }

        private string[] GetLogLines()
            => Browser.FindElement(By.TagName("textarea"))
            .GetAttribute("value")
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        private void TriggerCustomBubblingEvent(string elementId, string eventName)
        {
            var jsExecutor = (IJavaScriptExecutor)Browser;
            jsExecutor.ExecuteScript(
                $"document.getElementById('{elementId}').dispatchEvent(" +
                $"    new Event('{eventName}', {{ bubbles: true }})" +
                $")");
        }
    }
}
