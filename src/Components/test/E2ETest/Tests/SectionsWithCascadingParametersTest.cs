// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Components.E2ETest;
using OpenQA.Selenium;
using Microsoft.AspNetCore.Components.Sections;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class SectionsWithCascadingParametersTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public SectionsWithCascadingParametersTest
        (BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        Browser.MountTestComponent<BasicTestApp.SectionsTest.SectionsWithCascadingParameters>();
    }

    [Fact]
    public void CascadingParameterForSectionOutletContentIsDeterminedByMatchingSectionContent()
    {
        Browser.Equal("First Section with additional text for first section", () => Browser.Exists(By.TagName("p")).Text);

        Browser.FindElement(By.Id("render-second-section-content")).Click();

        Browser.Equal("Second Section with additional text for second section", () => Browser.Exists(By.TagName("p")).Text);
    }
}
