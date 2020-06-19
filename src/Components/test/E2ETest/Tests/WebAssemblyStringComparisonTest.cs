// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class WebAssemblyStringComparisonTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public WebAssemblyStringComparisonTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38126")]
        public void InvariantCultureWorksAsExpected()
        {
            Navigate(ServerPathBase, noReload: false);
            Browser.MountTestComponent<StringComparisonComponent>();

            var result = Browser.Exists(By.Id("results"));

            Assert.Equal("Ordinal: False Invariant: True", result.Text);
        }
    }
}
