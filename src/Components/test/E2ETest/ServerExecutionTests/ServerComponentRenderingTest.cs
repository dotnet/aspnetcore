// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
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
            var appElement = Browser.MountTestComponent<DispatchingComponent>();
            var result = appElement.FindElement(By.Id("result"));

            appElement.FindElement(By.Id("run-without-dispatch")).Click();

            Browser.Contains(
                $"{typeof(InvalidOperationException).FullName}: The current thread is not associated with the Dispatcher. Use InvokeAsync() to switch execution to the Dispatcher when triggering rendering or component state.",
                () => result.Text);
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19413")]
        public override void CanDispatchAsyncWorkToSyncContext()
            => base.CanDispatchAsyncWorkToSyncContext();
    }
}
