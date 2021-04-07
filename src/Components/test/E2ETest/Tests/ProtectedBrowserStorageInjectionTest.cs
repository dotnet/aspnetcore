// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Components.TestServer;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ProtectedBrowserStorageInjectionTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public ProtectedBrowserStorageInjectionTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
            Browser.MountTestComponent<ProtectedBrowserStorageInjectionComponent>();
        }

        [Fact]
        public void ThrowsWhenInjectingProtectedLocalStorageIfAndOnlyIfWebAssembly()
        {
            var messageElement = Browser.Exists(By.Id("message"));
            var injectLocalButton = Browser.Exists(By.Id("inject-local"));

            Browser.Equal("Waiting for injection...", () => messageElement.Text);

            injectLocalButton.Click();

            if (_serverFixture.ExecutionMode == ExecutionMode.Client)
            {
                Browser.Contains("cannot be used when running in a browser.", () => messageElement.Text);
            }
            else
            {
                Browser.Equal("Success!", () => messageElement.Text);
            }
        }

        [Fact]
        public void ThrowsWhenInjectingProtectedSessionStorageIfAndOnlyIfWebAssembly()
        {
            var messageElement = Browser.Exists(By.Id("message"));
            var injectSessionButton = Browser.Exists(By.Id("inject-session"));

            Browser.Equal("Waiting for injection...", () => messageElement.Text);

            injectSessionButton.Click();

            if (_serverFixture.ExecutionMode == ExecutionMode.Client)
            {
                Browser.Contains("cannot be used when running in a browser.", () => messageElement.Text);
            }
            else
            {
                Browser.Equal("Success!", () => messageElement.Text);
            }
        }
    }
}
