// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class WebAssemblyLocalizationTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public WebAssemblyLocalizationTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        [Theory(Skip = "https://github.com/dotnet/runtime/issues/38124")]
        [InlineData("en-US", "Hello!")]
        [InlineData("fr-FR", "Bonjour!")]
        public void CanSetCultureAndReadLocalizedResources(string culture, string message)
        {
            Navigate($"{ServerPathBase}/?culture={culture}", noReload: false);

            Browser.MountTestComponent<LocalizedText>();

            var cultureDisplay = Browser.Exists(By.Id("culture-name-display"));
            Assert.Equal($"Culture is: {culture}", cultureDisplay.Text);

            var messageDisplay = Browser.FindElement(By.Id("message-display"));
            Assert.Equal(message, messageDisplay.Text);
        }
    }
}
