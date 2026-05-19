// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public sealed class ComponentRenderingTest : ComponentRenderingTestBase
{
    public ComponentRenderingTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void CanDispatchAsyncWorkToSyncContext()
    {
        var appElement = Browser.MountTestComponent<DispatchingComponent>();
        var result = appElement.FindElement(By.Id("result"));

        appElement.FindElement(By.Id("run-async-with-dispatch")).Click();

        // this test assumes RendererSynchronizationContext optimization, which makes it synchronous execution.
        // with multi-threading runtime and WebAssemblyDispatcher `InvokeAsync` will be executed asynchronously ordering it differently.
        // See https://github.com/dotnet/aspnetcore/pull/52724#issuecomment-1895566632
        Browser.Equal("First Second Third Fourth Fifth", () => result.Text);
    }
}
