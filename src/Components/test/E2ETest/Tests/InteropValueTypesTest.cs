// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class InteropValueTypesTest(
    BrowserFixture browserFixture,
    ToggleExecutionModeServerFixture<Program> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<ToggleExecutionModeServerFixture<Program>>(browserFixture, serverFixture, output)
{
    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<InteropValueTypesComponent>();

        var interopButton = Browser.Exists(By.Id("btn-interop"));
        interopButton.Click();
        Browser.Exists(By.Id("done-with-interop"));
    }

    [Fact]
    public void CanRetrieveStoredGuidAsString()
    {
        var stringGuidElement = Browser.Exists(By.Id("string-get-by-interop"));

        Browser.NotEqual(string.Empty, () => stringGuidElement.Text);
        Browser.NotEqual(default, () => Guid.Parse(stringGuidElement.Text));
    }

    [Fact]
    public void CanRetrieveStoredGuidAsGuid()
    {
        var guidElement = Browser.Exists(By.Id("guid-get-by-interop"));

        Browser.NotEqual(default, () => Guid.Parse(guidElement.Text));
    }

    [Fact]
    public void CanRetrieveStoredGuidAsNullableGuid()
    {
        var nullableGuidElement = Browser.Exists(By.Id("nullable-guid-get-by-interop"));

        Browser.NotEqual(default, () => Guid.Parse(nullableGuidElement.Text));
    }

    [Fact]
    public void CanRetrieveNullAsNullableGuid()
    {
        var nullableGuidElement = Browser.Exists(By.Id("null-loaded-into-nullable"));

        Browser.Equal(true.ToString(), () => nullableGuidElement.Text);
    }
}
