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
                    new Node("orig2", "B edited"),
                    new Node("new3", "Inserted after") { IsNew = true },
                });
        }

        [Fact]
        public void CanDelete()
        {
            PerformTest(
                before: new[]
                {
                    new Node("orig1", "A"), // Will delete first
                    new Node("orig2", "B"),
                    new Node("orig3", "C"), // Will delete in middle
                    new Node("orig4", "D"),
                    new Node("orig5", "E"), // Will delete at end
                },
                after: new[]
                {
                    new Node("orig2", "B"),
                    new Node("orig4", "D edited"),
                });
        }

        [Fact]
        public void CanInsertUnkeyed()
        {
            PerformTest(
               before: new[]
               {
                    new Node("orig1", "A"),
                    new Node("orig2", "B"),
               },
               after: new[]
               {
                   new Node(null, "Inserted before") { IsNew = true },
                   new Node("orig1", "A edited"),
                   new Node(null, "Inserted between") { IsNew = true },
                   new Node("orig2", "B"),
                   new Node(null, "Inserted after") { IsNew = true },
               });
        }

        [Fact]
        public void CanDeleteUnkeyed()
        {
            PerformTest(
                before: new[]
                {
                    new Node(null, "A"), // Will delete first
                    new Node("orig2", "B"),
                    new Node(null, "C"), // Will delete in middle
                    new Node("orig4", "D"),
                    new Node(null, "E"), // Will delete at end
                },
                after: new[]
                {
                    new Node("orig2", "B edited"),
                    new Node("orig4", "D"),
                });
        }

        [Fact]
        public void CanReorder()
        {
            PerformTest(
                before: new[]
                {
                    new Node("keyA", "A",
                        new Node("keyA1", "A1"),
                        new Node("keyA2", "A2"),
                        new Node("keyA3", "A3")),
                    new Node("keyB", "B",
                        new Node("keyB1", "B1"),
                        new Node("keyB2", "B2"),
                        new Node("keyB3", "B3")),
                    new Node("keyC", "C",
                        new Node("keyC1", "C1"),
                        new Node("keyC2", "C2"),
                        new Node("keyC3", "C3")),
                },
                after: new[]
                {
                    // We're implicitly verifying that all the component instances were preserved,
                    // because we're not marking any with "IsNew = true"
                    new Node("keyC", "C", // Rotate all three (ABC->CAB)
                        // Swap first and last
                        new Node("keyC3", "C3"),
                        new Node("keyC2", "C2 edited"),
                        new Node("keyC1", "C1")),
                    new Node("keyA", "A",
                        // Swap first two
                        new Node("keyA2", "A2 edited"),
                        new Node("keyA1", "A1"),
                        new Node("keyA3", "A3")),
                    new Node("keyB", "B edited",
                        // Swap last two
                        new Node("keyB1", "B1"),
                        new Node("keyB3", "B3"),
                        new Node("keyB2", "B2 edited")),
                });
        }

        [Fact]
        public void CanReorderInsertDeleteAndEdit_WithAndWithoutKeys()
        {
            // This test is a complex bundle of many types of changes happening simultaneously
            PerformTest(
                before: new[]
                {
                    new Node("keyA", "A",
                        new Node("keyA1", "A1"),
                        new Node(null, "A2 unkeyed"),
                        new Node("keyA3", "A3"),
                        new Node("keyA4", "A4")),
                    new Node("keyB", "B",
                        new Node(null, "B1 unkeyed"),
                        new Node("keyB2", "B2"),
                        new Node("keyB3", "B3"),
                        new Node("keyB4", "B4")),
                    new Node("keyC", "C",
                        new Node("keyC1", "C1"),
                        new Node("keyC2", "C2"),
                        new Node("keyC3", "C3"),
                        new Node(null, "C4 unkeyed")),
                },
                after: new[]
                {
                    // Swapped A and C
                    new Node("keyC", "C",
                        // C1-4 were reordered
                        // C5 was inserted
                        new Node("keyC5", "C5 inserted") { IsNew = true },
                        new Node("keyC2", "C2"),
                        // C6 was inserted with no key
                        new Node(null, "C6 unkeyed inserted") { IsNew = true },
                        // C1 was edited
                        new Node("keyC1", "C1 edited"),
                        new Node("keyC3", "C3")
                        // C4 unkeyed was deleted
                        ),
                    // B was deleted
                    // D was inserted
                    new Node("keyD", "D inserted",
                        new Node("keyB1", "D1") { IsNew = true }, // Matches an old key, but treated as new because we don't move between parents
                        new Node("keyD2", "D2") { IsNew = true },
                        new Node(null, "D3 unkeyed") { IsNew = true })
                        { IsNew = true },
                    new Node("keyA", "A",
                        new Node("keyA1", "A1"),
                        // A2 (unkeyed) was edited
                        new Node(null, "A2 unkeyed edited"),
                        new Node("keyA3", "A3"),
                        // A4 was deleted
                        // A5 was inserted
                        new Node("keyA5", "A5 inserted") { IsNew = true }),
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
