// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using ComponentsApp.App.Pages;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    // By inheriting from ComponentRenderingTest, this test class also copies
    // all the test cases shared with client-side rendering

    public class ServerComponentRenderingTest : ComponentRenderingTest
    {
        public ServerComponentRenderingTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }

        [Fact]
        public void ThrowsIfRenderIsRequestedOutsideSyncContext()
        {
            var appElement = MountTestComponent<DispatchingComponent>();
            var result = appElement.FindElement(By.Id("result"));

            appElement.FindElement(By.Id("run-without-dispatch")).Click();

            WaitAssert.Contains(
                $"{typeof(InvalidOperationException).FullName}: The current thread is not associated with the renderer's synchronization context",
                () => result.Text);
        }
    }
}
