// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyTrimmingTest : ServerTestBase<BlazorWasmTestAppFixture<Program>>
{
    public WebAssemblyTrimmingTest(
        BrowserFixture browserFixture,
        BlazorWasmTestAppFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        _serverFixture.PathBase = "/subdir";
    }

    protected override void InitializeAsyncCore()
    {
        base.InitializeAsyncCore();
        Navigate(ServerPathBase);
    }

    [Fact]
    public void HotReloadTypesAreTrimmed_WhenPublishedWithTrimming()
    {
        if (!_serverFixture.TestTrimmedApps)
        {
            // In dev mode, hot reload types are expected to be present
            return;
        }

        var appElement = Browser.MountTestComponent<HotReloadTrimmingCheck>();

        // Hot reload manager type is present, but shallow type
        Browser.Equal("true", () => appElement.FindElement(By.Id("hot-reload-manager-found")).Text);

        // Verify that UpdateApplication method has been trimmed away
        Browser.Equal("false", () => appElement.FindElement(By.Id("update-application-found")).Text);
    }

    [Fact]
    public void MetricsTypesAreTrimmed_WhenPublishedWithTrimming()
    {
        if (!_serverFixture.TestTrimmedApps)
        {
            // In dev mode, metrics types are expected to be present
            return;
        }

        var appElement = Browser.MountTestComponent<MetricsTrimmingCheck>();

        // Verify that System.Diagnostics.Metrics.Meter.IsSupported is false
        Browser.Equal("false", () => appElement.FindElement(By.Id("is-supported")).Text);

        // There is trimmed empty type ComponentsMetrics
        Browser.Equal("true", () => appElement.FindElement(By.Id("metrics-found")).Text);

        // Verify that FailEventSync method has been trimmed away
        Browser.Equal("false", () => appElement.FindElement(By.Id("fail-event-sync-found")).Text);

        // There is trimmed empty type ComponentsActivitySource
        Browser.Equal("true", () => appElement.FindElement(By.Id("activity-source-found")).Text);

        // Verify that StartHandleEventActivity method has been trimmed away
        Browser.Equal("false", () => appElement.FindElement(By.Id("start-handle-event-activity-found")).Text);
    }

    [Fact]
    public void CustomEventArgsAreDeserialized_WhenPublishedWithTrimming()
    {
        // Regression test for https://github.com/microsoft/fast-blazor/issues/280, where custom
        // event args types were trimmed away in assemblies marked IsTrimmable=true, causing
        // deserialization to fail at runtime. The members required to deserialize a custom event
        // args type are preserved through the [EventHandler] attribute's DynamicallyAccessedMembers
        // annotation. This test runs against the trimmed BasicTestApp so it validates that the
        // custom event args type (and the members needed to JSON-deserialize it) survive trimming.
        var appElement = Browser.MountTestComponent<EventCustomArgsComponent>();

        appElement.FindElement(By.Id("register-testevent-with-createventargs-that-supplies-args")).Click();
        appElement.FindElement(By.Id("trigger-testevent-directly")).Click();

        // If the custom event args type had been trimmed, deserialization would fail and MyProp
        // would never be populated. Observing the value confirms the members were preserved.
        Browser.Equal(
            "Received testevent with args '{ MyProp=Native event target ID=test-event-target-child }'",
            () => GetLogLines(appElement).LastOrDefault());
    }

    private static string[] GetLogLines(IWebElement appElement)
        => appElement.FindElement(By.Id("test-log"))
            .GetDomProperty("value")
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
}
