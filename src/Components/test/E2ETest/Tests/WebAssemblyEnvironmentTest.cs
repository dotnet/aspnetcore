// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using GlobalizationWasmApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyEnvironmentTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public WebAssemblyEnvironmentTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void WebAssemblyEnvironment_Works()
    {
        Navigate($"{ServerPathBase}/");

        // Verify that the environment gets detected as 'Staging'.
        Browser.Equal("Staging", () => Browser.FindElement(By.Id("environment")).Text);
    }
}
