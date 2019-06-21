// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests
{
    public class RenderingModesTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public RenderingModesTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            _serverFixture.Environment = AspNetEnvironment.Development;
            _serverFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
        }

        [Fact]
        public void PrerenderedCircuitsShareSameCircuit()
        {
            Navigate("/startmodes/startmodeshost/prerendered");
            Browser.Equal("prerendered", () => Browser.FindElement(By.CssSelector("#prerendered-1>h3")).Text);
            Browser.Equal("prerendered", () => Browser.FindElement(By.CssSelector("#prerendered-2>h3")).Text);

            BeginInteractivity();
            var buttons = Browser.FindElements(By.CssSelector(".reveal"));
            foreach (var button in buttons)
            {
                button.Click();
            }

            var ids = Browser.FindElements(By.CssSelector(".circuit-id")).Select(e => e.Text).ToArray();

            Assert.Equal(2, ids.Length);
            Assert.Single(ids.Distinct());
        }

        [Fact]
        public void PrerenderedAndNonPrerenderedShareSameCircuit()
        {
            Navigate("/startmodes/startmodeshost/mixed");
            Browser.Equal("prerendered", () => Browser.FindElement(By.CssSelector("#prerendered-1>h3")).Text);
            Browser.Equal("prerendered", () => Browser.FindElement(By.CssSelector("#prerendered-2>h3")).Text);
            Browser.Exists(By.CssSelector("mixed1"));
            Browser.Exists(By.CssSelector("mixed2"));

            BeginInteractivity();
            Browser.Equal("no-prerendered", () => Browser.FindElement(By.CssSelector("#mixed-1>mixed1>h3")).Text);
            Browser.Equal("no-prerendered", () => Browser.FindElement(By.CssSelector("#mixed-2>mixed2>h3")).Text);

            var buttons = Browser.FindElements(By.CssSelector(".reveal"));
            foreach (var button in buttons)
            {
                button.Click();
            }

            var ids = Browser.FindElements(By.CssSelector(".circuit-id")).Select(e => e.Text).ToArray();

            Assert.Equal(4, ids.Length);
            Assert.Single(ids.Distinct());
        }

        [Fact]
        public void NonPrerenderedShareSameCircuit()
        {
            Navigate("/startmodes/startmodeshost/preregistered");
            Browser.Exists(By.CssSelector("preregistered1"));
            Browser.Exists(By.CssSelector("preregistered2"));

            BeginInteractivity();
            Browser.Equal("no-prerendered", () => Browser.FindElement(By.CssSelector("#preregistered-1>preregistered1>h3")).Text);
            Browser.Equal("no-prerendered", () => Browser.FindElement(By.CssSelector("#preregistered-2>preregistered2>h3")).Text);

            var buttons = Browser.FindElements(By.CssSelector(".reveal"));
            foreach (var button in buttons)
            {
                button.Click();
            }

            var ids = Browser.FindElements(By.CssSelector(".circuit-id")).Select(e => e.Text).ToArray();

            Assert.Equal(2, ids.Length);
            Assert.Single(ids.Distinct());
        }

        private void BeginInteractivity()
        {
            Browser.FindElement(By.Id("load-boot-script")).Click();
            Browser.True(() => ((IJavaScriptExecutor)Browser)
                .ExecuteScript("return window['__aspnetcore__testing__blazor__started__'];") == null ? false : true);
        }
    }
}
