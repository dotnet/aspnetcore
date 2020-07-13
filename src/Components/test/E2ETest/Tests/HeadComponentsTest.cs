// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests
{
    public class HeadComponentsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public HeadComponentsTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
            Browser.MountTestComponent<ModifyHeadComponent>();
        }

        [Fact]
        public void Title_AddsAndDiscardsChangesInReverseOrder()
        {
            var titleCount = 3;
            var titleButtonsById = Enumerable.Range(0, titleCount)
                .Select(i => (i, Browser.FindElement(By.Id($"button-title-{i}"))))
                .ToList();

            Assert.All(titleButtonsById, buttonById =>
            {
                var (id, button) = buttonById;
                button.Click();

                Browser.Equal($"Title {id}", () => Browser.Title);
            });
        }

        [Fact]
        public void Meta_AddsAndDiscardsChangesInReverseOrder()
        {
            var metaCount = 3;
            var metaButtonsById = Enumerable.Range(0, metaCount)
                .Select(i => (i, Browser.FindElement(By.Id($"button-meta-{i}"))))
                .ToList();

            Assert.All(metaButtonsById, buttonById =>
            {
                var (id, button) = buttonById;
                button.Click();

                var metaElement = Browser.FindElements(By.TagName("meta"))
                    .Where(e => e.GetAttribute("name").Equals("multiple-metas"))
                    .Single();

                Browser.Equal($"Meta {id}", () => metaElement.GetAttribute("content"));
            });
        }

        [Fact]
        public void Title_DeepestComponentHasPriority()
        {
            var nestedTitleButton = Browser.FindElement(By.Id("button-title-nested"));

            Browser.Equal("Basic test app", () => Browser.Title);

            nestedTitleButton.Click();

            Browser.Equal("Layer 4", () => Browser.Title);
        }

        [Fact]
        public void Meta_DeepestComponentHasPriority()
        {
            var nestedMetaButton = Browser.FindElement(By.Id("button-meta-nested"));

            Browser.Empty(FindNestedMetas);

            nestedMetaButton.Click();

            var nestedMetaElement = FindNestedMetas().Single();

            Browser.Equal("Layer 4", () => nestedMetaElement.GetAttribute("content"));

            IEnumerable<IWebElement> FindNestedMetas()
                => Browser.FindElements(By.TagName("meta"))
                          .Where(e => e.GetAttribute("http-equiv")?.Equals("nested-meta") ?? false);
        }

        [Fact]
        public void Link_NestedComponentsDoNotOverride()
        {
            var nestedLinkButton = Browser.FindElement(By.Id("button-link-nested"));

            Browser.Empty(FindNestedLinks);

            nestedLinkButton.Click();

            Browser.Equal(3, () => FindNestedLinks().Count());

            IEnumerable<IWebElement> FindNestedLinks()
                => Browser.FindElements(By.Id("nested-link"));
        }

        [Fact]
        public void Link_UpdatesSameElementWhenComponentPropertyChanged()
        {
            var linkAttributeInput1 = Browser.FindElement(By.Id("link-attr-input-1"));
            var linkAttributeInput2 = Browser.FindElement(By.Id("link-attr-input-2"));
            var linkElement = Browser.FindElement(By.Id("link-with-bindings"));

            Browser.Equal("First attribute", () => linkElement.GetAttribute("attr1"));
            Browser.Equal("Second attribute", () => linkElement.GetAttribute("attr2"));

            linkAttributeInput1.Clear();
            linkAttributeInput1.SendKeys("hello\n");

            Browser.Equal("hello", () => linkElement.GetAttribute("attr1"));
            Browser.Equal("Second attribute", () => linkElement.GetAttribute("attr2"));

            linkAttributeInput2.Clear();
            linkAttributeInput2.SendKeys("world\n");

            Browser.Equal("hello", () => linkElement.GetAttribute("attr1"));
            Browser.Equal("world", () => linkElement.GetAttribute("attr2"));
        }
    }
}
