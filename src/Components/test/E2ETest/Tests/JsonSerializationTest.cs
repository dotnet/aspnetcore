// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class JsonSerializationTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public JsonSerializationTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<JsonSerializationCases>();
        Browser.Exists(By.Id("json-serialization-cases"));
    }

    [Fact]
    public void JsonSerializationCasesWork()
    {
        Browser.Equal("Lord Smythe", () => Browser.Exists(By.Id("deserialized-name")).Text);
        Browser.Equal("68", () => Browser.Exists(By.Id("deserialized-age")).Text);
        Browser.Equal("Vexed", () => Browser.Exists(By.Id("deserialized-mood")).Text);
    }
}
