// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.JSInterop;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class KeyTest : BasicTestAppTestBase
    {
        public KeyTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, noReload: !_serverFixture.UsingAspNetHost);
        }

        [Fact]
        public void CanInsert()
        {
            PerformTest(
                before: new[]
                {
                    new Node("orig1", "A"),
                    new Node("orig2", "B"),
                },
                after: new[]
                {
                    new Node("new1", "Inserted before") { IsNew = true },
                    new Node("orig1", "A"),
                    new Node("new2", "Inserted between") { IsNew = true },
                    new Node("orig2", "B"),
                    new Node("new3", "Inserted after") { IsNew = true },
                });
        }

        private void PerformTest(Node[] before, Node[] after)
        {
            var rootBefore = new Node(null, "root", before);
            var rootAfter = new Node(null, "root", after);
            var jsonBefore = Json.Serialize(rootBefore);
            var jsonAfter = Json.Serialize(rootAfter);

            var appElem = MountTestComponent<KeyCasesComponent>();
            var textbox = appElem.FindElement(By.TagName("textarea"));
            var updateButton = appElem.FindElement(By.TagName("button"));

            SetTextAreaValueFast(textbox, jsonBefore);
            updateButton.Click();
            ValidateRenderedOutput(appElem, rootBefore, validatePreservation: false);

            SetTextAreaValueFast(textbox, jsonAfter);
            updateButton.Click();
            ValidateRenderedOutput(appElem, rootAfter, validatePreservation: true);
        }

        private static void ValidateRenderedOutput(IWebElement appElem, Node expectedRootNode, bool validatePreservation)
        {
            var actualRootElem = appElem.FindElement(By.CssSelector(".render-output > .node"));
            var actualRootNode = ReadNodeFromDOM(actualRootElem);
            AssertNodesEqual(expectedRootNode, actualRootNode, validatePreservation);
        }

        private static void AssertNodesEqual(Node expectedRootNode, Node actualRootNode, bool validatePreservation)
        {
            Assert.Equal(expectedRootNode.Label, actualRootNode.Label);

            if (validatePreservation)
            {
                Assert.Equal(expectedRootNode.IsNew, actualRootNode.IsNew);
            }

            Assert.Collection(
                actualRootNode.Children,
                expectedRootNode.Children.Select<Node, Action<Node>>(expectedChild =>
                    (actualChild => AssertNodesEqual(expectedChild, actualChild, validatePreservation))).ToArray());
        }

        private static Node ReadNodeFromDOM(IWebElement nodeElem)
        {
            var label = nodeElem.FindElement(By.ClassName("label")).Text;
            var childNodes = nodeElem
                .FindElements(By.XPath("*[@class='children']/*[@class='node']"));
            return new Node(key: null, label, childNodes.Select(ReadNodeFromDOM).ToArray())
            {
                IsNew = nodeElem.FindElement(By.ClassName("is-new")).Text == "true"
            };
        }

        private void SetTextAreaValueFast(IWebElement textAreaElementWithId, string value)
        {
            var javascript = (IJavaScriptExecutor)Browser;
            javascript.ExecuteScript($"document.getElementById('{textAreaElementWithId.GetAttribute("id")}').value = {Json.Serialize(value)}");
            textAreaElementWithId.SendKeys(" "); // So it fires the change event
        }

        class Node
        {
            public string Key { get; }
            public string Label { get; }
            public Node[] Children { get; }
            public bool IsNew { get; set; }

            public Node(string key, string label, params Node[] children)
            {
                Key = key;
                Label = label;
                Children = children ?? Array.Empty<Node>();
            }
        }
    }
}
