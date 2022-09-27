// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

// By inheriting from ComponentRenderingTest, this test class also copies
// all the test cases shared with client-side rendering

public class ServerComponentRenderingTest : ComponentRenderingTestBase
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
}
