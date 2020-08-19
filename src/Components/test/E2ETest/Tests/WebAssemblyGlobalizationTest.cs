// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETests.Tests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    // For now this is limited to server-side execution because we don't have the ability to set the
    // culture in client-side Blazor.
    // This type is internal since localization currently does not work.
    // Make it public onc https://github.com/dotnet/runtime/issues/38124 is resolved.
    internal class WebAssemblyGlobalizationTest : GlobalizationTest<ToggleExecutionModeServerFixture<Program>>
    {
        public WebAssemblyGlobalizationTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void SetCulture(string culture)
        {
            Navigate($"{ServerPathBase}/?culture={culture}", noReload: false);

            // That should have triggered a page load, so wait for the main test selector to come up.
            Browser.MountTestComponent<GlobalizationBindCases>();
            Browser.Exists(By.Id("globalization-cases"));

            var cultureDisplay = Browser.Exists(By.Id("culture-name-display"));
            Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);
        }
    }
}
