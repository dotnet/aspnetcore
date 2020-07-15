// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public void Title_DoesChangeDocumentTitle()
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
        public void Title_DeepestComponentHasPriority()
        {
            var nestedTitleButton = Browser.FindElement(By.Id("button-title-nested"));
            nestedTitleButton.Click();

            Browser.Equal("Layer 4", () => Browser.Title);
        }

        [Fact]
        public void Meta_AddsAndRemovesElements()
        {
            var metaCount = 3;
            var metaButtonsById = Enumerable.Range(0, metaCount)
                .Select(i => (i, Browser.FindElement(By.Id($"button-meta-{i}"))))
                .ToList();

            Assert.All(metaButtonsById, buttonById =>
            {
                var (id, button) = buttonById;
                button.Click();

                Browser.Exists(By.Id($"Meta {id}"));
            });

            Assert.All(metaButtonsById, buttonById =>
            {
                var (id, button) = buttonById;
                button.Click();

                Browser.DoesNotExist(By.Id($"Meta {id}"));
            });
        }

        [Fact]
        public void Meta_UpdatesSameElementWhenComponentPropertyChanged()
        {
            var metaAttributeInput1 = Browser.FindElement(By.Id("meta-attr-input-1"));
            var metaAttributeInput2 = Browser.FindElement(By.Id("meta-attr-input-2"));
            var metaElement = FindMetaElement();

            Browser.Equal("First attribute", () => metaElement.GetAttribute("attr1"));
            Browser.Equal("Second attribute", () => metaElement.GetAttribute("attr2"));

            metaAttributeInput1.Clear();
            metaAttributeInput1.SendKeys("hello\n");
            metaElement = FindMetaElement();

            Browser.Equal("hello", () => metaElement.GetAttribute("attr1"));
            Browser.Equal("Second attribute", () => metaElement.GetAttribute("attr2"));

            metaAttributeInput2.Clear();
            metaAttributeInput2.SendKeys("world\n");
            metaElement = FindMetaElement();

            Browser.Equal("hello", () => metaElement.GetAttribute("attr1"));
            Browser.Equal("world", () => metaElement.GetAttribute("attr2"));

            IWebElement FindMetaElement() => Browser.FindElements(By.Id("meta-with-bindings")).Single();
        }

        [Fact]
        public void Link_AddsAndRemovesElements()
        {
            var linkCount = 3;
            var linkButtonsById = Enumerable.Range(0, linkCount)
                .Select(i => (i, Browser.FindElement(By.Id($"button-link-{i}"))))
                .ToList();

            Assert.All(linkButtonsById, buttonById =>
            {
                var (id, button) = buttonById;
                button.Click();

                Browser.Exists(By.Id($"Link {id}"));
            });

            Assert.All(linkButtonsById, buttonById =>
            {
                var (id, button) = buttonById;
                button.Click();

                Browser.DoesNotExist(By.Id($"Link {id}"));
            });
        }

        [Fact]
        public void Link_UpdatesSameElementWhenComponentPropertyChanged()
        {
            var linkAttributeInput1 = Browser.FindElement(By.Id("link-attr-input-1"));
            var linkAttributeInput2 = Browser.FindElement(By.Id("link-attr-input-2"));
            var linkElement = FindLinkElement();

            Browser.Equal("First attribute", () => linkElement.GetAttribute("attr1"));
            Browser.Equal("Second attribute", () => linkElement.GetAttribute("attr2"));

            linkAttributeInput1.Clear();
            linkAttributeInput1.SendKeys("hello\n");
            linkElement = FindLinkElement();

            Browser.Equal("hello", () => linkElement.GetAttribute("attr1"));
            Browser.Equal("Second attribute", () => linkElement.GetAttribute("attr2"));

            linkAttributeInput2.Clear();
            linkAttributeInput2.SendKeys("world\n");
            linkElement = FindLinkElement();

            Browser.Equal("hello", () => linkElement.GetAttribute("attr1"));
            Browser.Equal("world", () => linkElement.GetAttribute("attr2"));

            IWebElement FindLinkElement() => Browser.FindElements(By.Id("link-with-bindings")).Single();
        }
    }
}
