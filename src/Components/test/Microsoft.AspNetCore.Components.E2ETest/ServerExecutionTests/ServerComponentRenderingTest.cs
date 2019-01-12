// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.Browser.Rendering;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using OpenQA.Selenium;
using System.Threading.Tasks;
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
        public async Task ThrowsIfRenderIsRequestedOutsideSyncContext()
        {
            var appElement = MountTestComponent<DispatchingComponent>();
            var result = appElement.FindElement(By.Id("result"));

            appElement.FindElement(By.Id("run-without-dispatch")).Click();

            // Because the preceding operation triggers an error, there's no way
            // to get UI output from it, so we have to just wait a moment
            await Task.Delay(500);
            appElement.FindElement(By.Id("show-result")).Click();

            WaitAssert.Contains(
                $"{typeof(RemoteRendererException).FullName}: The current thread is not associated with the renderer's synchronization context",
                () => result.Text);
        }

        [Fact]
        public void CanDispatchRenderToSyncContext()
        {
            var appElement = MountTestComponent<DispatchingComponent>();
            var result = appElement.FindElement(By.Id("result"));

            appElement.FindElement(By.Id("run-with-dispatch")).Click();

            WaitAssert.Equal("Success (completed synchronously)", () => result.Text);
        }
    }
}
