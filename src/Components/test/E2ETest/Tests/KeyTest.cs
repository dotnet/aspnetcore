// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class KeyTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
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
        Navigate(ServerPathBase);
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

    [Fact]
    public async Task CanRetainFocusWhileMovingTextBox()
    {
        var appElem = Browser.MountTestComponent<ReorderingFocusComponent>();
        Func<IWebElement> textboxFinder = () => appElem.FindElement(By.CssSelector(".incomplete-items .item-1 input[type=text]"));
        var textToType = "Hello there!";
        var expectedTextTyped = "";

        textboxFinder().Clear();

        // On each keystroke, the boxes will be shuffled. The text will only
        // be inserted correctly if focus is retained.
        textboxFinder().Click();
        while (textToType.Length > 0)
        {
            var nextChar = textToType.Substring(0, 1);
            textToType = textToType.Substring(1);
            expectedTextTyped += nextChar;

            // Send keys to whatever has focus
            new Actions(Browser).SendKeys(nextChar).Perform();
            Browser.Equal(expectedTextTyped, () => textboxFinder().GetDomProperty("value"));

            // We delay between typings to ensure the events aren't all collapsed into one.
            await Task.Delay(50);
        }

        // Verify that after all this, we can still move the edited item
        // This was broken originally because of unexpected event-handling behavior
        // in Chrome (it raised events recursively)
        appElem.FindElement(
            By.CssSelector(".incomplete-items .item-1 input[type=checkbox]")).Click();
        Browser.Equal(expectedTextTyped, () => appElem
            .FindElement(By.CssSelector(".complete-items .item-1 input[type=text]"))
            .GetDomProperty("value"));
    }

    [Fact]
    public void CanUpdateCheckboxStateWhileMovingIt()
    {
        var appElem = Browser.MountTestComponent<ReorderingFocusComponent>();
        Func<IWebElement> checkboxFinder = () => appElem.FindElement(By.CssSelector(".item-2 input[type=checkbox]"));
        Func<IEnumerable<bool>> incompleteItemStates = () => appElem
            .FindElements(By.CssSelector(".incomplete-items input[type=checkbox]"))
            .Select(elem => elem.Selected);
        Func<IEnumerable<bool>> completeItemStates = () => appElem
            .FindElements(By.CssSelector(".complete-items input[type=checkbox]"))
            .Select(elem => elem.Selected);

        // Verify initial state
        Browser.Equal(new[] { false, false, false, false, false }, incompleteItemStates);
        Browser.Equal(Array.Empty<bool>(), completeItemStates);

        // Check a box; see it moves and becomes the sole checked item
        checkboxFinder().Click();
        Browser.True(() => checkboxFinder().Selected);
        Browser.Equal(new[] { false, false, false, false }, incompleteItemStates);
        Browser.Equal(new[] { true }, completeItemStates);

        // Also uncheck it; see it moves and becomes unchecked
        checkboxFinder().Click();
        Browser.False(() => checkboxFinder().Selected);
        Browser.Equal(new[] { false, false, false, false, false }, incompleteItemStates);
        Browser.Equal(Array.Empty<bool>(), completeItemStates);
    }

    private void PerformTest(Node[] before, Node[] after)
    {
        var rootBefore = new Node(null, "root", before);
        var rootAfter = new Node(null, "root", after);
        var jsonBefore = JsonSerializer.Serialize(rootBefore, TestJsonSerializerOptionsProvider.Options);
        var jsonAfter = JsonSerializer.Serialize(rootAfter, TestJsonSerializerOptionsProvider.Options);

        var appElem = Browser.MountTestComponent<KeyCasesComponent>();
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
        javascript.ExecuteScript(
            $"document.getElementById('{textAreaElementWithId.GetDomAttribute("id")}').value = {JsonSerializer.Serialize(value, TestJsonSerializerOptionsProvider.Options)}");
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
