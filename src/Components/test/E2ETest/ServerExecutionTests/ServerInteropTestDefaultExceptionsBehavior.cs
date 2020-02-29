// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ServerInteropTestDefaultExceptionsBehavior : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
    {
        public ServerInteropTestDefaultExceptionsBehavior(
            BrowserFixture browserFixture,
            BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: true);
            Browser.MountTestComponent<InteropComponent>();
        }

        [Fact]
        public void DotNetExceptionDetailsAreNotLoggedByDefault()
        {
            // Arrange
            var expectedValues = new Dictionary<string, string>
            {
                ["AsyncThrowSyncException"] = GetExpectedMessage("AsyncThrowSyncException"),
                ["AsyncThrowAsyncException"] = GetExpectedMessage("AsyncThrowAsyncException"),
            };

            var actualValues = new Dictionary<string, string>();

            // Act
            var interopButton = Browser.FindElement(By.Id("btn-interop"));
            interopButton.Click();

            var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(10))
                .Until(d => d.FindElement(By.Id("done-with-interop")));

            foreach (var expectedValue in expectedValues)
            {
                var currentValue = Browser.FindElement(By.Id(expectedValue.Key));
                actualValues.Add(expectedValue.Key, currentValue.Text);
            }

            // Assert
            foreach (var expectedValue in expectedValues)
            {
                Assert.Equal(expectedValue.Value, actualValues[expectedValue.Key]);
            }

            string GetExpectedMessage(string method) =>
                $"\"There was an exception invoking '{method}' on assembly 'BasicTestApp'. For more details turn on " +
                $"detailed exceptions in '{typeof(CircuitOptions).Name}.{nameof(CircuitOptions.DetailedErrors)}'\"";
        }
    }
}
