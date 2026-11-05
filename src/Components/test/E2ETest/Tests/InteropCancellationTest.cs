// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class InteropCancellationTest(
    BrowserFixture browserFixture,
    ToggleExecutionModeServerFixture<Program> serverFixture,
    ITestOutputHelper output) : ServerTestBase<ToggleExecutionModeServerFixture<Program>>(browserFixture, serverFixture, output)
{
    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<InteropCancellationComponent>();
        Browser.Equal("ready", () => Browser.FindElement(By.Id("cancellation-ready")).Text);
    }

    [Fact]
    public void AbortCancelsRunningDotNetOperationWithoutCancelingAnotherCall()
    {
        Click("start-first");
        Equal("first-managed", "running");
        Click("start-second");
        Equal("second-managed", "running");
        Click("abort-first");
        Equal("first-managed", "canceled in .NET");
        Equal("first-js", "AbortError");
        Equal("first-listeners", "0");
        Equal("second-managed", "running");
        Equal("second-js", "pending");
        Equal("second-listeners", "1");

        Click("release-second");
        Equal("second-managed", "completed");
        Equal("second-js", "completed");
        Equal("second-listeners", "0");
        Click("abort-second");
        Equal("second-managed", "completed");
        Equal("second-js", "completed");
    }

    [Fact]
    public void AbortReasonStaysInJavaScriptAndNeedNotBeSerializable()
    {
        Click("start-first");
        Equal("first-managed", "running");
        Click("abort-first-with-reason");
        Equal("first-managed", "canceled in .NET");
        Equal("first-js", "custom reason preserved");
        Equal("first-listeners", "0");
    }

    [Fact]
    public void AlreadyAbortedSignalDoesNotInvokeDotNet()
    {
        Click("start-preaborted");
        Equal("preaborted-js", "AbortError");
        Equal("preaborted-managed", "not started");
        Equal("preaborted-listeners", "0");
        Click("start-ordinary");
        Equal("ordinary-js", "ordinary result");
    }

    [Fact]
    public void IgnoringCancellationCanCompleteWithoutSecondPromiseSettlement()
    {
        Click("start-ignored");
        Equal("ignored-managed", "running");
        Click("abort-ignored");
        Equal("ignored-js", "AbortError");
        Equal("ignored-listeners", "0");
        Equal("ignored-managed", "running");
        Click("release-ignored");
        Equal("ignored-managed", "completed after cancellation");
        Equal("ignored-js", "AbortError");
        Click("start-ordinary");
        Equal("ordinary-js", "ordinary result");
    }

    [Fact]
    public void FailurePreservesInteropErrorAndRemovesAbortListener()
    {
        Click("start-failure");
        Equal("failure-managed", "running");
        Click("release-failure");
        Browser.True(() =>
        {
            var message = Browser.FindElement(By.Id("failure-js")).Text;
            return message.Contains("Expected invocation failure", StringComparison.Ordinal)
                || message.Contains("There was an exception invoking 'Run'", StringComparison.Ordinal);
        });
        Equal("failure-listeners", "0");
        Click("start-ordinary");
        Equal("ordinary-js", "ordinary result");
    }

    [Fact]
    public void StaticInvocationReceivesCancellationToken()
    {
        Click("start-static");
        Equal("static-js", "static token supplied");
    }

    private void Click(string id) => Browser.Exists(By.Id(id)).Click();

    private void Equal(string id, string expected) => Browser.Equal(expected, () => Browser.FindElement(By.Id(id)).Text);
}
