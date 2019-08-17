// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class MultipleComponentsTest : BasicTestAppTestBase
    {
        private const string MarkerPattern = ".*?<!--Blazor:(.*?)-->.*?";

        public MultipleComponentsTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }

        [Fact]
        public void CanRenderMultipleRootComponents()
        {
            Navigate("/prerendered/multiple-components");

            var greets = Browser.FindElements(By.CssSelector(".greet-wrapper .greet")).Select(e => e.Text).ToArray();

            Assert.Equal(4, greets.Length); // 1 statically rendered + 3 prerendered
            Assert.Single(greets, "Hello John");
            Assert.Equal(3, greets.Where(g => string.Equals("Hello", g)).Count()); // 3 prerendered
            var content = Browser.FindElement(By.Id("test-container")).GetAttribute("innerHTML");
            var markers = ReadMarkers(content);
            var componentSequence = markers.Select(m => m.Item1.PrerenderId != null).ToArray();
            var expectedComponentSequence = new bool[]
            {
                // true means it was a prerendered component.
                true,
                false,
                false,
                false,
                true,
                false,
                true,
            };
            Assert.Equal(expectedComponentSequence, componentSequence);

            // Once connected, output changes
            BeginInteractivity();

            Browser.Exists(By.CssSelector("h3.interactive"));
            var updatedGreets = Browser.FindElements(By.CssSelector(".greet-wrapper .greet")).Select(e => e.Text).ToArray();
            Assert.Equal(7, updatedGreets.Where(g => string.Equals("Hello Alfred", g)).Count());
        }

        private (ServerComponentMarker, ServerComponentMarker)[] ReadMarkers(string content)
        {
            content = content.Replace("\r\n", "");
            var matches = Regex.Matches(content, MarkerPattern);
            var markers = matches.Select(s => JsonSerializer.Deserialize<ServerComponentMarker>(
                s.Groups[1].Value,
                ServerComponentSerializationSettings.JsonSerializationOptions));

            var prerenderMarkers = markers.Where(m => m.PrerenderId != null).GroupBy(p => p.PrerenderId).Select(g => (g.First(), g.Skip(1).First())).ToArray();
            var nonPrerenderMarkers = markers.Where(m => m.PrerenderId == null).Select(g => (g, (ServerComponentMarker)default)).ToArray();

            return prerenderMarkers.Concat(nonPrerenderMarkers).OrderBy(m => m.Item1.Sequence).ToArray();
        }

        private void BeginInteractivity()
        {
            Browser.FindElement(By.Id("load-boot-script")).Click();
        }
    }
}
