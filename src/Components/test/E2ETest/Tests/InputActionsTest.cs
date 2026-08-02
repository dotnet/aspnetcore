// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class InputFocusTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public InputFocusTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        // On WebAssembly, page reloads are expensive so skip if possible
        Navigate(ServerPathBase);
    }

    protected virtual IWebElement MountInputActionsComponent()
        => Browser.MountTestComponent<InputFocusComponent>();

    [Fact]
    public void InputElementsGetFocusedSuccessfully()
    {
        var appElement = MountInputActionsComponent();
        Browser.Exists(By.ClassName("input-group"));
        var inputGroups = appElement.FindElements(By.ClassName("input-group"));

        foreach (var group in inputGroups)
        {
            var expected = group.FindElement(By.ClassName("input-control"));
            var button = group.FindElement(By.TagName("button"));
            button.Click();

            Browser.Equal(expected, () => Browser.SwitchTo().ActiveElement());
        }
    }
}
