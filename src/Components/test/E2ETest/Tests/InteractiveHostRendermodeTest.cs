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

public class InteractiveHostRendermodeTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public InteractiveHostRendermodeTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public virtual string ExpectedValue => "Interactive Webassembly";

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<HostRenderMode>();
    }

    [Fact]
    public void InteractiveHostRenderMode_Works()
    {
        var target = Browser.Exists(By.Id("host-render-mode"));
        Browser.Contains(ExpectedValue, () => target.Text);
    }
}

