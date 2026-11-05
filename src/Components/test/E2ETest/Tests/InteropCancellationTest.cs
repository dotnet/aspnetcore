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
        Equal("cancellation-ready", "ready");
    }

    [Fact]
    public void SignalMapsToTokenWithoutChangingInvocationOutcomeOrOtherTokens()
    {
        Click("start-first");
        Equal("first-managed", "running");
        Click("start-second");
        Equal("second-managed", "running");
        Click("abort-first");
        Equal("first-managed", "canceled in .NET");
        Equal("first-js", "handler handled cancellation");
        Equal("first-listeners", "1");
        Equal("second-managed", "running");
        Click("release-second");
        Equal("second-js", "completed");
    }

    [Fact]
    public void PreAbortedSignalStillInvokesMethodWithCanceledToken()
    {
        Click("start-preaborted");
        Equal("preaborted-managed", "entered with canceled token");
        Equal("preaborted-js", "pre-canceled result");
    }

    [Fact]
    public void IgnoringTokenLeavesPromisePendingAndAllowsNormalCompletion()
    {
        Click("start-ignored");
        Equal("ignored-managed", "running");
        Click("abort-ignored");
        Equal("ignored-js", "pending");
        Click("release-ignored");
        Equal("ignored-js", "completed after cancellation");
        Equal("ignored-managed", "completed after cancellation");
    }

    [Fact]
    public void ReferenceCanOutliveInvocationAndBeReused()
    {
        Click("start-retained");
        Equal("retained-js", "returned normally");
        Equal("retained-listeners", "1");
        Click("abort-retained");
        Equal("retained-managed", "canceled after return");
        Click("reuse-retained");
        Equal("retained-js", "reused: true");
        Click("dispose-retained");
        Equal("retained-js", "reference disposed");
        Equal("retained-listeners", "0");
    }

    [Fact]
    public void IncomingReferenceDisposalRemovesListenerWithoutCancelingToken()
    {
        Click("start-retained");
        Equal("retained-js", "returned normally");
        Click("dispose-retained");
        Equal("retained-js", "reference disposed");
        Equal("retained-listeners", "0");
        Click("abort-retained");
        Equal("retained-managed", "retained");
    }

    [Fact]
    public void FailureDoesNotEndReferenceLifetimeOrAlterInteropError()
    {
        Click("start-failure");
        Browser.True(() =>
        {
            var message = Browser.FindElement(By.Id("failure-js")).Text;
            return message.Contains("Expected invocation failure", StringComparison.Ordinal)
                || message.Contains("There was an exception invoking 'Receive'", StringComparison.Ordinal);
        });
        Equal("failure-listeners", "1");
    }

    [Fact]
    public void TokenMapsToNativeSignalThatOutlivesInvocation()
    {
        Click("start-outgoing");
        Equal("outgoing-js", "native signal retained");
        Equal("outgoing-managed", "returned normally");
        Click("cancel-outgoing");
        Equal("outgoing-js", "aborted in JS");
    }

    [Fact]
    public void PreCanceledTokenProducesAbortedSignalAcceptedByFetch()
    {
        Click("start-outgoing-pre");
        Equal("outgoing-js", "aborted on entry");
        Equal("outgoing-listeners", "AbortError");
        Equal("outgoing-managed", "returned normally");
    }

    [Fact]
    public void OutgoingReferenceDisposalDoesNotAbortSignal()
    {
        Click("start-outgoing");
        Equal("outgoing-js", "native signal retained");
        Click("dispose-outgoing");
        Equal("outgoing-managed", "reference disposed");
        Click("cancel-outgoing");
        Equal("outgoing-js", "native signal retained");
    }

    [Fact]
    public void InvocationControlTokenDoesNotBecomeSignalOrStopJavaScript()
    {
        Click("start-control");
        Equal("control-js", "JS running with uncanceled signal");
        Click("cancel-control");
        Equal("control-managed", "managed wait canceled");
        Equal("control-js", "JS running with uncanceled signal");
        Click("release-control");
        Equal("control-js", "JS completed normally");
    }

    private void Click(string id) => Browser.Exists(By.Id(id)).Click();

    private void Equal(string id, string expected) => Browser.Equal(expected, () => Browser.FindElement(By.Id(id)).Text);
}
