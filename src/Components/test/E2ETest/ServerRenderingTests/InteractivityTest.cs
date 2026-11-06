// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

[CollectionDefinition(nameof(InteractivityTest), DisableParallelization = true)]
public class InteractivityTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public InteractivityTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Theory]
    [InlineData("server", "")]
    [InlineData("wasm", "")]
    [InlineData("auto", "")]
    [InlineData("server", "text")]
    [InlineData("wasm", "text")]
    [InlineData("auto", "text")]
    [InlineData("server", "attribute")]
    [InlineData("wasm", "attribute")]
    [InlineData("server", "element")]
    [InlineData("wasm", "element")]
    [InlineData("server", "extra")]
    [InlineData("wasm", "extra")]
    [InlineData("server", "missing")]
    [InlineData("wasm", "missing")]
    [InlineData("server", "markup")]
    [InlineData("wasm", "markup")]
    [InlineData("server", "namespace")]
    [InlineData("wasm", "namespace")]
    [InlineData("server", "order")]
    [InlineData("wasm", "order")]
    [InlineData("server", "attribute-order")]
    [InlineData("wasm", "attribute-order")]
    [InlineData("server", "root-attribute")]
    [InlineData("wasm", "root-attribute")]
    public void PrerenderedDom_WithInitializersUsesWholeRootFallbackForMismatches(string platform, string mismatch)
    {
        Navigate($"{ServerPathBase}/prerendered-dom?suppress-autostart&platform={platform}&mismatch={mismatch}");
        var root = Browser.Exists(By.Id("dom-first-root"));
        var secondRoot = Browser.Exists(By.Id("dom-second-root"));
        var input = Browser.Exists(By.Id("dom-first-input"));
        input.Clear();
        input.SendKeys("Before activation");
        var textarea = Browser.Exists(By.Id("dom-first-textarea"));
        textarea.Clear();
        textarea.SendKeys("Edited textarea");
        Browser.Exists(By.Id("dom-first-checkbox")).Click();
        new SelectElement(Browser.Exists(By.Id("dom-first-select"))).SelectByValue("second");
        var secondInput = Browser.Exists(By.Id("dom-second-input"));
        secondInput.Clear();
        secondInput.SendKeys("Other root");
        var inputBound = Browser.Exists(By.Id("dom-first-input-bound"));
        inputBound.Clear();
        inputBound.SendKeys("Before input");
        input.Click();

        var js = (IJavaScriptExecutor)Browser;
        if (mismatch == "namespace")
        {
            js.ExecuteScript("""
                const original = document.querySelector('#dom-first-root svg text');
                const replacement = document.createElementNS('http://www.w3.org/1999/xhtml', 'text');
                replacement.textContent = original.textContent;
                original.replaceWith(replacement);
                """);
        }
        else if (mismatch == "attribute-order")
        {
            js.ExecuteScript("""
                const root = arguments[0];
                const id = root.id;
                root.removeAttribute('id');
                root.setAttribute('id', id);
                """, root);
        }
        // WebDriver cannot set a selection range or observe synchronous removal records.
        js.ExecuteScript("""
            arguments[0].setSelectionRange(2, 7);
            document.getElementById('dom-first-scroll').scrollTop = 90;
            if (!customElements.get('prerendered-dom-probe')) {
                customElements.define('prerendered-dom-probe', class extends HTMLElement {
                    connectedCallback() { this.connectionCount = (this.connectionCount || 0) + 1; }
                });
            }
            window.prerenderedDomAnimation = document.getElementById('dom-first-animation').getAnimations()[0];
            window.prerenderedDomStylesheet = document.getElementById('dom-first-stylesheet').sheet;
            window.prerenderedDomRemovals = 0;
            window.prerenderedDomObserver = new MutationObserver(records => {
                for (const record of records) {
                    for (const removed of record.removedNodes) {
                        if (removed === arguments[1] || removed.contains(arguments[1])) {
                            window.prerenderedDomRemovals++;
                        }
                    }
                }
            });
            window.prerenderedDomObserver.observe(document, { childList: true, subtree: true });
            """, input, root);

        // Clicking the start button would itself move focus out of the input.
        js.ExecuteScript("callBlazorStart()");
        Browser.True(() => Browser.FindElement(By.Id("dom-first-ready")).Text is "Server" or "WebAssembly");
        Browser.True(() => Browser.FindElement(By.Id("dom-second-ready")).Text is "Server" or "WebAssembly");

        var shouldPreserveRoot = mismatch is "" or "attribute-order";
        var currentRoot = Browser.FindElement(By.Id("dom-first-root"));
        Assert.Equal(shouldPreserveRoot, root.Equals(currentRoot));
        Assert.Equal(shouldPreserveRoot, input.Equals(Browser.FindElement(By.Id("dom-first-input"))));
        Assert.Equal(shouldPreserveRoot ? "Before activation" : "Initial", Browser.FindElement(By.Id("dom-first-input")).GetDomProperty("value"));
        Assert.Equal(shouldPreserveRoot ? "Edited textarea" : "Initial", Browser.FindElement(By.Id("dom-first-textarea")).GetDomProperty("value"));
        Assert.Equal(shouldPreserveRoot, Browser.FindElement(By.Id("dom-first-checkbox")).Selected);
        Assert.Equal(shouldPreserveRoot ? "second" : "first", new SelectElement(Browser.FindElement(By.Id("dom-first-select"))).SelectedOption.GetDomProperty("value"));
        Assert.Equal(shouldPreserveRoot ? 90L : 0L, js.ExecuteScript("return document.getElementById('dom-first-scroll').scrollTop"));
        Assert.Equal(shouldPreserveRoot ? 0L : 1L, js.ExecuteScript("return window.prerenderedDomRemovals"));
        Assert.Equal(shouldPreserveRoot ? 2L : 3L, js.ExecuteScript("return window.prerenderedDomScriptRuns"));
        Assert.Equal("Initial", Browser.FindElement(By.Id("dom-first-model")).Text);
        Assert.Equal("Initial", Browser.FindElement(By.Id("dom-first-input-model")).Text);
        Assert.Equal(shouldPreserveRoot ? "Before input" : "Initial", Browser.FindElement(By.Id("dom-first-input-bound")).GetDomProperty("value"));
        Assert.Equal(secondRoot, Browser.FindElement(By.Id("dom-second-root")));
        Assert.Equal("Other root", secondInput.GetDomProperty("value"));

        if (shouldPreserveRoot)
        {
            Assert.Equal(input, Browser.SwitchTo().ActiveElement());
            Assert.Equal("2", input.GetDomProperty("selectionStart"));
            Assert.Equal("7", input.GetDomProperty("selectionEnd"));
            Assert.Equal("1", Browser.FindElement(By.Id("dom-first-custom")).GetDomProperty("connectionCount"));
            Assert.Equal(true, js.ExecuteScript("return window.prerenderedDomAnimation === document.getElementById('dom-first-animation').getAnimations()[0]"));
            Assert.Equal(true, js.ExecuteScript("return !!window.prerenderedDomStylesheet && window.prerenderedDomStylesheet === document.getElementById('dom-first-stylesheet').sheet"));
        }
        js.ExecuteScript("window.prerenderedDomObserver.disconnect()");

        AssertNormalRenderingAndBindingAfterActivation(expectedSelectedValue: shouldPreserveRoot ? "second" : "first");
    }

    private void AssertNormalRenderingAndBindingAfterActivation(string expectedSelectedValue)
    {
        Browser.Click(By.Id("dom-first-increment"));
        Browser.Equal("Count: 1", () => Browser.FindElement(By.Id("dom-first-count")).Text);
        Browser.Equal("Hello 1 world", () => Browser.FindElement(By.Id("dom-first-adjacent")).Text);
        Browser.Equal("Nested 1", () => Browser.FindElement(By.Id("dom-first-child")).Text);
        Browser.Click(By.Id("dom-first-add-option"));
        Assert.Equal(expectedSelectedValue, new SelectElement(Browser.FindElement(By.Id("dom-first-select"))).SelectedOption.GetDomProperty("value"));
        Browser.Click(By.Id("dom-first-select-third"));
        Browser.Equal("third", () => new SelectElement(Browser.FindElement(By.Id("dom-first-select"))).SelectedOption.GetDomProperty("value"));
        Browser.Click(By.Id("dom-first-stop-propagation"));
        Browser.Equal("1/0", () => Browser.FindElement(By.Id("dom-first-events")).Text);
        Browser.Click(By.Id("dom-first-focus"));
        Browser.Equal(Browser.FindElement(By.Id("dom-first-input")), () => Browser.SwitchTo().ActiveElement());
        var currentInput = Browser.FindElement(By.Id("dom-first-input"));
        currentInput.Clear();
        currentInput.SendKeys("After activation" + Keys.Tab);
        Browser.Equal("After activation", () => Browser.FindElement(By.Id("dom-first-model")).Text);
        Browser.Equal("After activation", () => Browser.FindElement(By.Id("dom-first-textarea")).GetDomProperty("value"));
        var inputBound = Browser.FindElement(By.Id("dom-first-input-bound"));
        inputBound.Clear();
        inputBound.SendKeys("After input");
        Browser.Equal("After input", () => Browser.FindElement(By.Id("dom-first-input-model")).Text);
        Browser.Click(By.Id("dom-first-toggle"));
        Browser.DoesNotExist(By.Id("dom-first-child"));
        Browser.Click(By.Id("dom-first-toggle"));
        Browser.Equal("Nested 1", () => Browser.FindElement(By.Id("dom-first-child")).Text);
    }

    [Theory]
    [InlineData("server", "select-text-options", false, "45 min")]
    [InlineData("wasm", "select-text-options", false, "45 min")]
    [InlineData("server", "select-text-options-matching", true, "30 min")]
    [InlineData("wasm", "select-text-options-matching", true, "30 min")]
    [InlineData("server", "select-dynamic-options", true, "30 min")]
    [InlineData("wasm", "select-dynamic-options", true, "30 min")]
    [InlineData("server", "select-selected-option", true, "30 min")]
    [InlineData("wasm", "select-selected-option", true, "30 min")]
    [InlineData("server", "select-listbox", false, "45 min")]
    [InlineData("wasm", "select-listbox", false, "45 min")]
    [InlineData("server", "select-empty-value", false, "")]
    [InlineData("wasm", "select-empty-value", false, "")]
    [InlineData("server", "select-unmatched-value", false, "")]
    [InlineData("wasm", "select-unmatched-value", false, "")]
    [InlineData("server", "select-no-value", true, "30 min")]
    [InlineData("wasm", "select-no-value", true, "30 min")]
    [InlineData("server", "select-disabled", true, "5 min")]
    [InlineData("wasm", "select-disabled", true, "5 min")]
    [InlineData("server", "select-disabled-optgroup", true, "30 min")]
    [InlineData("wasm", "select-disabled-optgroup", true, "30 min")]
    [InlineData("server", "select-multiple", false, "5 min,45 min")]
    [InlineData("wasm", "select-multiple", false, "5 min,45 min")]
    [InlineData("server", "select-multiple-matching", true, "30 min")]
    [InlineData("wasm", "select-multiple-matching", true, "30 min")]
    [InlineData("server", "select-duplicate-values", false, "45 min")]
    [InlineData("wasm", "select-duplicate-values", false, "45 min")]
    public void PrerenderedDom_ChecksSelectDefaultsBeforePreservingUserSelection(string platform, string scenario, bool shouldPreserveSelect, string expectedValues)
    {
        Navigate($"{ServerPathBase}/prerendered-dom?suppress-autostart&platform={platform}&scenario={scenario}");
        var root = Browser.Exists(By.Id("position-root"));
        var select = Browser.Exists(By.Id("position-select"));
        var note = Browser.Exists(By.Id("position-note"));
        if (select.Enabled)
        {
            var selection = new SelectElement(select);
            if (selection.IsMultiple)
            {
                selection.DeselectAll();
            }
            selection.SelectByText("30 min");
        }
        note.Clear();
        note.SendKeys("Before activation");

        ((IJavaScriptExecutor)Browser).ExecuteScript("callBlazorStart()");
        Browser.True(() => Browser.FindElement(By.Id("position-ready")).Text is "Server" or "WebAssembly");

        Assert.Equal(expectedValues, GetSelectedValues());
        Assert.Equal(shouldPreserveSelect, select.Equals(Browser.FindElement(By.Id("position-select"))));
        Assert.Equal(root, Browser.FindElement(By.Id("position-root")));
        Assert.Equal(note, Browser.FindElement(By.Id("position-note")));
        Assert.Equal("Before activation", note.GetDomProperty("value"));
        if (scenario == "select-duplicate-values")
        {
            Assert.Equal("First 45 min", new SelectElement(Browser.FindElement(By.Id("position-select"))).SelectedOption.Text);
        }

        Browser.Click(By.Id("position-select-update"));
        Browser.Equal("15 min", GetSelectedValues);
        Browser.Click(By.Id("position-increment"));
        Browser.Equal("Count: 1", () => Browser.FindElement(By.Id("position-increment")).Text);

        string GetSelectedValues() => string.Join(",", new SelectElement(Browser.FindElement(By.Id("position-select")))
            .AllSelectedOptions.Select(option => option.GetDomProperty("value")));
    }

    [Theory]
    [InlineData("server", "nested")]
    [InlineData("wasm", "nested")]
    [InlineData("server", "input-default")]
    [InlineData("wasm", "input-default")]
    [InlineData("server", "textarea-default")]
    [InlineData("wasm", "textarea-default")]
    [InlineData("server", "textarea-text")]
    [InlineData("wasm", "textarea-text")]
    [InlineData("server", "textarea-text-matching")]
    [InlineData("wasm", "textarea-text-matching")]
    [InlineData("server", "markup-textarea")]
    [InlineData("wasm", "markup-textarea")]
    [InlineData("server", "remove-leading")]
    [InlineData("wasm", "remove-leading")]
    [InlineData("server", "add-leading")]
    [InlineData("wasm", "add-leading")]
    [InlineData("server", "remove-middle")]
    [InlineData("wasm", "remove-middle")]
    [InlineData("server", "insert-middle")]
    [InlineData("wasm", "insert-middle")]
    [InlineData("server", "text")]
    [InlineData("wasm", "text")]
    [InlineData("server", "component")]
    [InlineData("wasm", "component")]
    [InlineData("server", "empty-component")]
    [InlineData("wasm", "empty-component")]
    [InlineData("server", "markup-tree")]
    [InlineData("wasm", "markup-tree")]
    [InlineData("server", "markup-siblings")]
    [InlineData("wasm", "markup-siblings")]
    [InlineData("server", "table")]
    [InlineData("wasm", "table")]
    public void PrerenderedDom_ReplacesOnlyMismatchedPositions(string platform, string scenario)
    {
        Navigate($"{ServerPathBase}/prerendered-dom?suppress-autostart&platform={platform}&scenario={scenario}");
        var root = Browser.Exists(By.Id("position-root"));
        var scope = Browser.Exists(By.Id("position-scope"));
        var note = Browser.Exists(By.Id("position-note"));
        var footer = Browser.Exists(By.Id("position-footer"));
        Browser.Exists(By.Id("position-footer-note")).SendKeys("Bob");
        note.Clear();
        note.SendKeys("Bob");
        IWebElement item = null;
        IWebElement lastItem = null;
        IWebElement nestedNote = null;
        if (scenario is "remove-middle" or "insert-middle")
        {
            item = Browser.Exists(By.Id("position-item-1"));
            lastItem = Browser.Exists(By.Id("position-item-3"));
        }
        if (scenario is "input-default" or "textarea-default" or "textarea-text" or "textarea-text-matching" or "markup-textarea")
        {
            nestedNote = Browser.Exists(By.Id("position-changed-input"));
            nestedNote.Clear();
            nestedNote.SendKeys("Bob");
        }
        else if (scenario is "markup-tree" or "markup-siblings" or "component")
        {
            nestedNote = Browser.Exists(By.Id(scenario == "component" ? "position-child-note" : "position-markup-note"));
            nestedNote.Clear();
            nestedNote.SendKeys("Bob");
        }
        IWebElement[] combinedNotes = [];
        if (scenario == "component")
        {
            combinedNotes = [Browser.Exists(By.Id("position-combined-note-True")), Browser.Exists(By.Id("position-combined-note-False"))];
            foreach (var combinedNote in combinedNotes)
            {
                combinedNote.Clear();
                combinedNote.SendKeys("Bob");
            }
        }
        note.Click();
        var js = (IJavaScriptExecutor)Browser;
        // WebDriver cannot start activation without moving focus or compare a replaced text node.
        js.ExecuteScript("""
            window.prerenderedPositionText = document.getElementById('position-scope').firstChild;
            callBlazorStart();
            """);
        Browser.True(() => Browser.FindElement(By.Id("position-ready")).Text is "Server" or "WebAssembly");
        Assert.Equal(root, Browser.FindElement(By.Id("position-root")));
        Assert.Equal(scope, Browser.FindElement(By.Id("position-scope")));
        Assert.Equal(footer, Browser.FindElement(By.Id("position-footer")));
        Assert.Equal("AliceBob", Browser.FindElement(By.Id("position-footer-note")).GetDomProperty("value"));
        var currentNote = Browser.FindElement(By.Id("position-note"));
        var preservesNote = scenario is not ("remove-leading" or "add-leading" or "empty-component");
        Assert.Equal(preservesNote, note.Equals(currentNote));
        Assert.Equal(preservesNote ? "Bob" : "Alice", currentNote.GetDomProperty("value"));
        if (preservesNote)
        {
            Assert.Equal(note, Browser.SwitchTo().ActiveElement());
        }
        if (scenario is "remove-middle" or "insert-middle")
        {
            Assert.Equal(item, Browser.FindElement(By.Id("position-item-1")));
            Assert.NotEqual(lastItem, Browser.FindElement(By.Id("position-item-3")));
            Assert.Equal(scenario == "remove-middle" ? 2 : 3, Browser.FindElements(By.CssSelector("#position-list > span")).Count);
        }
        if (scenario is "input-default" or "textarea-default" or "textarea-text" or "markup-textarea")
        {
            var changedInput = Browser.FindElement(By.Id("position-changed-input"));
            Assert.NotEqual(nestedNote, changedInput);
            Assert.Equal("Carol", changedInput.GetDomProperty("value"));
        }
        else if (nestedNote is not null)
        {
            var currentNestedNote = Browser.FindElement(By.Id(scenario switch
            {
                "component" => "position-child-note",
                "textarea-text-matching" => "position-changed-input",
                _ => "position-markup-note"
            }));
            Assert.Equal(nestedNote, currentNestedNote);
            Assert.Equal("Bob", currentNestedNote.GetDomProperty("value"));
        }
        if (scenario == "text")
        {
            Assert.Contains("Hello Carol", scope.Text);
            Assert.Equal(false, js.ExecuteScript("return window.prerenderedPositionText === document.getElementById('position-scope').firstChild"));
        }
        if (scenario == "nested")
        {
            Browser.Equal("Ready", () => Browser.FindElement(By.CssSelector("strong#position-change")).Text);
        }
        if (scenario == "component")
        {
            Browser.Exists(By.CssSelector("strong#position-child-change"));
            Browser.Equal("Before Carol after", () => Browser.FindElement(By.Id("position-combined-True")).Text);
            Browser.Equal("Before Alice after", () => Browser.FindElement(By.Id("position-combined-False")).Text);
            foreach (var combinedNote in combinedNotes)
            {
                Assert.Equal(combinedNote, Browser.FindElement(By.Id(combinedNote.GetDomProperty("id"))));
                Assert.Equal("Bob", combinedNote.GetDomProperty("value"));
            }
            Browser.Click(By.Id("position-child-increment"));
            Browser.Equal("Child: 1", () => Browser.FindElement(By.Id("position-child-increment")).Text);
        }
        if (scenario == "table")
        {
            Browser.Exists(By.CssSelector("#position-table > tr"));
        }
        Browser.Click(By.Id("position-increment"));
        Browser.Equal("Count: 1", () => Browser.FindElement(By.Id("position-increment")).Text);
        Browser.Click(By.Id("position-toggle"));
        Browser.DoesNotExist(By.Id("position-scope"));
        Assert.Equal(footer, Browser.FindElement(By.Id("position-footer")));
        Browser.Click(By.Id("position-toggle"));
        Browser.Equal("Alice", () => Browser.FindElement(By.Id("position-note")).GetDomProperty("value"));
        Assert.Equal("AliceBob", Browser.FindElement(By.Id("position-footer-note")).GetDomProperty("value"));
    }

    [Theory]
    [InlineData("server", "script-order")]
    [InlineData("wasm", "script-order")]
    [InlineData("server", "unchanged-script-changed-target")]
    [InlineData("wasm", "unchanged-script-changed-target")]
    [InlineData("server", "custom-element-order")]
    [InlineData("wasm", "custom-element-order")]
    [InlineData("server", "unchanged-custom-element-changed-target")]
    [InlineData("wasm", "unchanged-custom-element-changed-target")]
    [InlineData("server", "nested-script-order")]
    [InlineData("wasm", "nested-script-order")]
    [InlineData("server", "unchanged-nested-script-changed-target")]
    [InlineData("wasm", "unchanged-nested-script-changed-target")]
    public void PrerenderedDom_InitializersBindToCurrentNodes(string platform, string scenario)
    {
        VerifyPrerenderedDomInitializer(platform, scenario,
            shouldPreserveRoot: scenario is not ("unchanged-script-changed-target" or "unchanged-custom-element-changed-target" or "unchanged-nested-script-changed-target"));
    }

    [Theory]
    [InlineData("server", "matching-script")]
    [InlineData("wasm", "matching-script")]
    [InlineData("server", "matching-custom-element")]
    [InlineData("wasm", "matching-custom-element")]
    [InlineData("server", "nested-script-subtree")]
    [InlineData("wasm", "nested-script-subtree")]
    public void PrerenderedDom_InitializerControlsKeepMatchingNodes(string platform, string scenario)
    {
        VerifyPrerenderedDomInitializer(platform, scenario, shouldPreserveRoot: true);
    }

    private void VerifyPrerenderedDomInitializer(string platform, string scenario, bool shouldPreserveRoot)
    {
        Navigate($"{ServerPathBase}/prerendered-dom?suppress-autostart&platform={platform}&scenario={scenario}");
        var root = Browser.Exists(By.Id("position-root"));
        var footer = Browser.Exists(By.Id("position-footer"));
        Browser.Exists(By.Id("position-footer-note")).SendKeys("Bob");
        var js = (IJavaScriptExecutor)Browser;
        // WebDriver cannot register a custom-element lifecycle callback or compare captured DOM references.
        js.ExecuteScript("""
            if (!customElements.get('prerendered-position-connector')) {
                customElements.define('prerendered-position-connector', class extends HTMLElement {
                    connectedCallback() {
                        window.prerenderedPositionInitializerRuns = (window.prerenderedPositionInitializerRuns || 0) + 1;
                        const target = document.getElementById('position-target');
                        window.prerenderedPositionInitializedTarget = target;
                        target.addEventListener('click', function () {
                            window.prerenderedPositionNativeClicks = (window.prerenderedPositionNativeClicks || 0) + 1;
                            document.getElementById('position-native-status').textContent = 'Clicked';
                        });
                    }
                });
            }
            callBlazorStart();
            """);
        Browser.True(() => Browser.FindElement(By.Id("position-ready")).Text is "Server" or "WebAssembly");
        Browser.Exists(By.Id("position-target")).Click();
        Assert.Equal("Clicked", Browser.FindElement(By.Id("position-native-status")).Text);
        Assert.Equal(1L, js.ExecuteScript("return window.prerenderedPositionNativeClicks"));
        Assert.Equal(true, js.ExecuteScript("return window.prerenderedPositionInitializedTarget === document.getElementById('position-target')"));
        Assert.Equal(shouldPreserveRoot, root.Equals(Browser.FindElement(By.Id("position-root"))));
        var currentFooter = Browser.FindElement(By.Id("position-footer"));
        Assert.Equal(shouldPreserveRoot, footer.Equals(currentFooter));
        Assert.Equal(shouldPreserveRoot ? "AliceBob" : "Alice", Browser.FindElement(By.Id("position-footer-note")).GetDomProperty("value"));
        var expectedInitializationCount = scenario is "matching-script" or "matching-custom-element" or "custom-element-order" ? 1L : 2L;
        Assert.Equal(expectedInitializationCount, js.ExecuteScript("return window.prerenderedPositionInitializerRuns"));

        Browser.Click(By.Id("position-increment"));
        Browser.Equal("Count: 1", () => Browser.FindElement(By.Id("position-increment")).Text);
        Browser.Click(By.Id("position-toggle"));
        Browser.DoesNotExist(By.Id("position-scope"));
        Assert.Equal(currentFooter, Browser.FindElement(By.Id("position-footer")));
        Browser.Click(By.Id("position-toggle"));
        Browser.Exists(By.Id("position-target")).Click();
        Assert.Equal("Clicked", Browser.FindElement(By.Id("position-native-status")).Text);
        Assert.Equal(2L, js.ExecuteScript("return window.prerenderedPositionNativeClicks"));
        Assert.Equal(currentFooter, Browser.FindElement(By.Id("position-footer")));
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    [InlineData("auto")]
    public void PrerenderedDom_WithoutPrerenderingUsesNormalRendering(string platform)
    {
        Navigate($"{ServerPathBase}/prerendered-dom?suppress-autostart&platform={platform}&noPrerender=true");
        Browser.DoesNotExist(By.Id("dom-first-root"));
        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.True(() => Browser.FindElement(By.Id("dom-first-ready")).Text is "Server" or "WebAssembly");
        Browser.Click(By.Id("dom-first-increment"));
        Browser.Equal("Count: 1", () => Browser.FindElement(By.Id("dom-first-count")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    [InlineData("auto")]
    public void PrerenderedDom_PreservesDocumentSelectionAcrossTextFrames(string platform)
    {
        Navigate($"{ServerPathBase}/prerendered-dom?suppress-autostart&platform={platform}");
        Browser.Exists(By.Id("dom-first-selection"));
        var js = (IJavaScriptExecutor)Browser;
        // WebDriver cannot select an exact range spanning text-frame boundaries.
        js.ExecuteScript("""
            const text = document.getElementById('dom-first-selection').firstChild;
            const range = document.createRange();
            range.setStart(text, 2);
            range.setEnd(text, 10);
            window.getSelection().removeAllRanges();
            window.getSelection().addRange(range);
            callBlazorStart();
            """);
        Browser.True(() => Browser.FindElement(By.Id("dom-first-ready")).Text is "Server" or "WebAssembly");
        Assert.Equal("llo 0 wo", js.ExecuteScript("return window.getSelection().toString()"));
    }

    [Theory]
    [InlineData("server", true)]
    [InlineData("wasm", false)]
    public void PrerenderedDom_EditFormRetainsNormalBindingAndSubmission(string platform, bool shouldPreserveInput)
    {
        Navigate($"{ServerPathBase}/prerendered-dom?suppress-autostart&platform={platform}&includeForm=true");
        var root = Browser.Exists(By.Id("dom-first-root"));
        var form = root.FindElement(By.TagName("form"));
        Assert.Equal("post", form.GetDomAttribute("method"));
        var outsideInput = Browser.Exists(By.Id("dom-first-input"));
        outsideInput.Clear();
        outsideInput.SendKeys("Outside the form");
        var input = Browser.Exists(By.Id("dom-first-nested-input"));
        var prerenderedName = input.GetDomAttribute("name");
        Assert.False(string.IsNullOrEmpty(prerenderedName));
        input.Clear();
        input.SendKeys("Before activation");
        ((IJavaScriptExecutor)Browser).ExecuteScript("callBlazorStart()");
        Browser.True(() => Browser.FindElement(By.Id("dom-first-ready")).Text is "Server" or "WebAssembly");
        Assert.Equal(root, Browser.FindElement(By.Id("dom-first-root")));
        var currentForm = Browser.FindElement(By.Id("dom-first-root")).FindElement(By.TagName("form"));
        Assert.Equal(shouldPreserveInput, form.Equals(currentForm));
        Assert.Equal(shouldPreserveInput ? "post" : null, currentForm.GetDomAttribute("method"));
        Assert.Equal(outsideInput, Browser.FindElement(By.Id("dom-first-input")));
        Assert.Equal("Outside the form", outsideInput.GetDomProperty("value"));
        Assert.Equal(shouldPreserveInput, input.Equals(Browser.FindElement(By.Id("dom-first-nested-input"))));
        input = Browser.FindElement(By.Id("dom-first-nested-input"));
        Assert.Equal(shouldPreserveInput ? prerenderedName : null, input.GetDomAttribute("name"));
        Assert.Equal(shouldPreserveInput ? "Before activation" : "Nested", input.GetDomProperty("value"));
        input.Clear();
        input.SendKeys("After activation" + Keys.Tab);
        Browser.Click(By.Id("dom-first-submit"));
        Browser.Equal("After activation", () => Browser.FindElement(By.Id("dom-first-submitted")).Text);
    }

    [Fact]
    public void PrerenderedDom_AutoPreservesNodesBeforeWebAssemblyResourcesAreAvailable()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();
        try
        {
            PrerenderedDom_WithInitializersUsesWholeRootFallbackForMismatches("auto", "");
            Browser.Equal("Server", () => Browser.FindElement(By.Id("dom-first-ready")).Text);
        }
        finally
        {
            UnblockWebAssemblyResourceLoad();
        }
    }

    [Fact]
    public void PrerenderedDom_AutoPreservesNodesWhenWebAssemblyIsCached()
    {
        Navigate($"{ServerPathBase}/prerendered-dom?platform=wasm");
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("dom-first-ready")).Text);
        PrerenderedDom_WithInitializersUsesWholeRootFallbackForMismatches("auto", "");
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("dom-first-ready")).Text);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    [InlineData("auto")]
    public void PrerenderedDom_PreservesPlayingVideo(string platform)
    {
        Navigate($"{ServerPathBase}/prerendered-dom?suppress-autostart&platform={platform}");
        var video = Browser.Exists(By.Id("dom-first-video"));
        var js = (IJavaScriptExecutor)Browser;
        // A browser-produced video stream avoids a codec or external media-file dependency.
        Assert.Null(js.ExecuteAsyncScript("""
            const done = arguments[arguments.length - 1];
            const canvas = document.createElement('canvas');
            canvas.width = canvas.height = 16;
            const context = canvas.getContext('2d');
            let frame = 0;
            function draw() {
                context.fillStyle = frame++ % 2 ? 'red' : 'blue';
                context.fillRect(0, 0, 16, 16);
                window.prerenderedDomVideoFrame = requestAnimationFrame(draw);
            }
            draw();
            const video = document.getElementById('dom-first-video');
            window.prerenderedDomVideoStream = canvas.captureStream(30);
            video.srcObject = window.prerenderedDomVideoStream;
            video.play().then(() => done(null), error => done(error.message));
            """));
        try
        {
            Browser.True(() => (bool)js.ExecuteScript("return document.getElementById('dom-first-video').currentTime > 0"));
            var before = js.ExecuteScript("return document.getElementById('dom-first-video').currentTime");
            js.ExecuteScript("callBlazorStart()");
            Browser.True(() => Browser.FindElement(By.Id("dom-first-ready")).Text is "Server" or "WebAssembly");
            Assert.Equal(video, Browser.FindElement(By.Id("dom-first-video")));
            Assert.False(bool.Parse(video.GetDomProperty("paused")));
            Browser.True(() => (bool)js.ExecuteScript("return document.getElementById('dom-first-video').currentTime > arguments[0]", before));
        }
        finally
        {
            js.ExecuteScript("""
                cancelAnimationFrame(window.prerenderedDomVideoFrame);
                window.prerenderedDomVideoStream.getTracks().forEach(track => track.stop());
                """);
        }
    }

    [Fact]
    public void CanRenderInteractiveServerComponent()
    {
        // '2' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?server=2");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);

        Browser.Click(By.Id("increment-server"));

        Browser.Equal("2", () => Browser.FindElement(By.Id("count-server")).Text);
    }

    [Fact]
    public void CanRenderInteractiveServerComponentFromRazorClassLibrary()
    {
        // '3' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?server-shared=3");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server-shared")).Text);

        Browser.Click(By.Id("increment-server-shared"));

        Browser.Equal("3", () => Browser.FindElement(By.Id("count-server-shared")).Text);
    }

    [Fact]
    public void CanRenderInteractiveWebAssemblyComponentFromRazorClassLibrary()
    {
        // '4' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?wasm-shared=4");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm-shared")).Text);

        Browser.Click(By.Id("increment-wasm-shared"));

        Browser.Equal("4", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
    }

    [Fact]
    public void CanRenderInteractiveWebAssemblyComponentFromRazorClassLibraryThatIsNotExplicitlyReferenced()
    {
        Navigate($"{ServerPathBase}/not-explicitly-referenced-in-wasm-code");

        // The element with id success is only rendered when webassembly has successfully loaded the component.
        Browser.Exists(By.Id("success"));
    }

    [Fact]
    public void CanRenderInteractiveServerAndWebAssemblyComponentsAtTheSameTime()
    {
        // '3' and '5' configure the increment amounts.
        Navigate($"{ServerPathBase}/interactive?server-shared=3&wasm-shared=5");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm-shared")).Text);

        Browser.Click(By.Id("increment-server-shared"));
        Browser.Click(By.Id("increment-wasm-shared"));

        Browser.Equal("3", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("5", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_Server(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, serverIncrement: 3));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-server"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);

        var countServerElem = Browser.FindElement(By.Id("count-server"));
        Browser.Equal("0", () => countServerElem.Text);

        Browser.Click(By.Id("increment-server"));
        Browser.Equal("3", () => countServerElem.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_WebAssembly(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, webAssemblyIncrement: 4));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-wasm"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);

        var countWasmElem = Browser.FindElement(By.Id("count-wasm"));
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-wasm"));
        Browser.Equal("4", () => countWasmElem.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_WebAssembly_WithInitiallyNullUnionParameter(bool prerender)
    {
        // Keep the union as the first and only typed parameter in a fresh WebAssembly runtime.
        // Deserializing another parameter first can initialize the shared JSON options.
        Navigate($"{InteractiveCallsiteUrl(prerender)}&union=true");
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("active null", () => Browser.FindElement(By.Id("value-union")).Text);
            Browser.Equal("Static", () => Browser.FindElement(By.Id("render-mode-union")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-union")).Text);
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-union")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("value-union"));
            Browser.DoesNotExist(By.Id("count-union"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-union")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-union")).Text);
        Browser.Equal("active null", () => Browser.FindElement(By.Id("value-union")).Text);
        Browser.Equal("0", () => Browser.FindElement(By.Id("count-union")).Text);

        Browser.Click(By.Id("increment-union"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-union")).Text);

        Browser.Click(By.Id("set-union-value"));
        Browser.Equal("int:42", () => Browser.FindElement(By.Id("value-union")).Text);
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-union")).Text);

        Browser.Click(By.Id("increment-union"));
        Browser.Equal("2", () => Browser.FindElement(By.Id("count-union")).Text);

        Browser.Click(By.Id("clear-union-value"));
        Browser.Equal("active null", () => Browser.FindElement(By.Id("value-union")).Text);
        Browser.Equal("2", () => Browser.FindElement(By.Id("count-union")).Text);

        Browser.Click(By.Id("increment-union"));
        Browser.Equal("3", () => Browser.FindElement(By.Id("count-union")).Text);

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [InlineData("server", "", "active null")]
    [InlineData("server", "&unionValue=42", "int:42")]
    [InlineData("server", "&unionText=hello", "string:hello")]
    [InlineData("wasm", "", "active null")]
    [InlineData("wasm", "&unionValue=42", "int:42")]
    [InlineData("wasm", "&unionText=hello", "string:hello")]
    public void CanUseCallSiteRenderMode_WithUnionParameter(string mode, string valueQuery, string expectedValue)
    {
        Navigate($"{InteractiveCallsiteUrl(prerender: true)}&union=true&unionMode={mode}{valueQuery}");

        Browser.Equal("Static", () => Browser.FindElement(By.Id("render-mode-union")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-union")).Text);
        Browser.Equal(expectedValue, () => Browser.FindElement(By.Id("value-union")).Text);

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal(mode == "server" ? "Server" : "WebAssembly", () => Browser.FindElement(By.Id("render-mode-union")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-union")).Text);
        Browser.Equal(expectedValue, () => Browser.FindElement(By.Id("value-union")).Text);
        Browser.Equal("0", () => Browser.FindElement(By.Id("count-union")).Text);

        Browser.Click(By.Id("increment-union"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-union")).Text);

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_ServerAndWebAssembly(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, serverIncrement: 10, webAssemblyIncrement: 11));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-server"));
            Browser.DoesNotExist(By.Id("count-wasm"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);

        var countServerElem = Browser.FindElement(By.Id("count-server"));
        var countWasmElem = Browser.FindElement(By.Id("count-wasm"));
        Browser.Equal("0", () => countServerElem.Text);
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-server"));
        Browser.Equal("10", () => countServerElem.Text);
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-wasm"));
        Browser.Equal("11", () => countWasmElem.Text);
        Browser.Equal("10", () => countServerElem.Text);
    }

    private const string AddServerId = "add-server-counter-link";
    private const string AddServerPrerenderedId = "add-server-counter-prerendered-link";
    private const string AddWebAssemblyId = "add-webassembly-counter-link";
    private const string AddWebAssemblyPrerenderedId = "add-webassembly-counter-prerendered-link";
    private const string AddAutoPrerenderedId = "add-auto-counter-prerendered-link";

    public static readonly TheoryData<string[]> AddCounterLinkSequences = new()
    {
        // One component
        new[] { AddServerPrerenderedId },
        new[] { AddWebAssemblyPrerenderedId },

        // Multiple components, mixing all combinations of Server/WebAssembly and prerendered/non-prerendered
        new[] { AddServerPrerenderedId, AddWebAssemblyId, AddWebAssemblyPrerenderedId, AddServerId },
    };

    [Theory]
    [InlineData(AddServerId)]
    [InlineData(AddServerPrerenderedId)]
    [InlineData(AddWebAssemblyId)]
    [InlineData(AddWebAssemblyPrerenderedId)]
    public void DynamicallyAddedSsrComponent_CanBecomeInteractive_AfterEnhancedNavigation(string addCounterLinkId)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(addCounterLinkId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-0")).Text);

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void MultipleDynamicallyAddedSsrComponents_CanBecomeInteractive_AfterEnhancedNavigation(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void DynamicallyAddedSsrComponents_CanBecomeInteractive_AfterStreamingRenderingCompletes(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));

            // To verify that components use the most up-to-date parameters when they become interactive, we
            // perform SSR updates with new parameter values.
            // The first counter will have an increment amount of 1, the second 2, etc.
            for (var j = 0; j < i; j++)
            {
                Browser.Click(By.Id($"update-counter-link-{i}"));
            }

            if (addCounterLinkIds[i].Contains("prerendered"))
            {
                Browser.Equal("False", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
                Browser.Click(By.Id($"increment-{i}"));
                Browser.Equal("0", () => Browser.FindElement(By.Id($"count-{i}")).Text);
            }
            else
            {
                // Non-prerendered components won't produce any output until they become interactive.
                // We verify this by ensuring that the "action links" exist on the page,
                // but the interactive component does not.
                Browser.Exists(By.Id($"remove-counter-link-{i}"));
                Browser.DoesNotExist(By.Id($"is-interactive-{i}"));
            }
        }

        Browser.Click(By.Id("stop-streaming-link"));

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal($"{i + 1}", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors(entry => !entry.Message.Contains("Initializer"));
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void DynamicallyAddedSsrComponents_CanBecomeInteractive_AfterInitialPageLoadCompletes(string[] addCounterLinkIds)
    {
        // Navigate directly to the page with streaming enabled so that we can defer the page load.
        Navigate($"{ServerPathBase}/streaming-interactivity?ShouldStream=True");
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Equal("loading", () => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.document.readyState;"));

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));

            // To verify that components use the most up-to-date parameters when they become interactive, we
            // perform SSR updates with new parameter values.
            // The first counter will have an increment amount of 1, the second 2, etc.
            for (var j = 0; j < i; j++)
            {
                Browser.Click(By.Id($"update-counter-link-{i}"));
            }

            if (addCounterLinkIds[i].Contains("prerendered"))
            {
                Browser.Equal("False", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
                Browser.Click(By.Id($"increment-{i}"));
                Browser.Equal("0", () => Browser.FindElement(By.Id($"count-{i}")).Text);
            }
            else
            {
                // Non-prerendered components won't produce any output until they become interactive.
                // We verify this by ensuring that the "action links" exist on the page,
                // but the interactive component does not.
                Browser.Exists(By.Id($"remove-counter-link-{i}"));
                Browser.DoesNotExist(By.Id($"is-interactive-{i}"));
            }
        }

        Browser.Click(By.Id("stop-streaming-link"));

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal($"{i + 1}", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    private void EnableClassicInitializers(IWebDriver browser, bool skipNavigation)
    {
        if (!skipNavigation)
        {
            browser.Navigate().GoToUrl($"{new Uri(_serverFixture.RootUri, ServerPathBase)}/");
        }
        ((IJavaScriptExecutor)browser).ExecuteScript("sessionStorage.setItem('enable-classic-initializers', 'true')");
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_CanReceiveSsrParameterUpdates_FromEnhancedNavigation(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            Browser.Click(By.Id($"update-counter-link-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"increment-amount-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("3", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_CanReceiveSsrParameterUpdates_FromStreamingRenderingUpdate(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        // Components don't become interactive during streaming rendering, so we need to
        // add then via enhanced navigation first
        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
        }

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            Browser.Click(By.Id($"update-counter-link-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"increment-amount-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("3", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        Browser.Click(By.Id("stop-streaming-link"));

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_GetReinitialized_WhenNoKeyIsProvided_AfterReceivingSsrParameterUpdates_FromEnhancedNavigation(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Click(By.Id("disable-keys-link"));
        Browser.Exists(By.Id("enable-keys-link"));

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"update-counter-link-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"increment-amount-{i}")).Text);
            Browser.Equal("0", () => Browser.FindElement(By.Id($"count-{i}")).Text); // Resets back to 0 because the parameter changed

            // Ensure that interactivity still works, and the correct parameters have been supplied.
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            for (var j = 0; j < addCounterLinkIds.Length; j++)
            {
                if (j == i)
                {
                    continue;
                }

                // Ensure that other components didn't get reset; their parameters did not change.
                Browser.NotEqual("0", () => Browser.FindElement(By.Id($"count-{j}")).Text);
            }
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_GetReinitialized_WhenNoKeyIsProvided_AfterReceivingSsrParameterUpdates_FromStreamingRenderingUpdate(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Click(By.Id("disable-keys-link"));
        Browser.Exists(By.Id("enable-keys-link"));

        // Components don't become interactive during streaming rendering, so we need to
        // add then via enhanced navigation first
        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"update-counter-link-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"increment-amount-{i}")).Text);
            Browser.Equal("0", () => Browser.FindElement(By.Id($"count-{i}")).Text); // Resets back to 0 because the parameter changed

            // Ensure that interactivity still works, and the correct parameters have been supplied.
            // Note that while SSR'd components don't become interactive during stream rendering,
            // this streaming update replaced an already-interactive component. To ensure that supplying
            // unchanged parameters behaves the same way as supplying updated parameters, we
            // check that the component is still interactive.
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            for (var j = 0; j < addCounterLinkIds.Length; j++)
            {
                if (j == i)
                {
                    continue;
                }

                // Ensure that other components didn't get reset; their parameters did not change.
                Browser.NotEqual("0", () => Browser.FindElement(By.Id($"count-{j}")).Text);
            }
        }

        Browser.Click(By.Id("stop-streaming-link"));

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_CanGetDisposed_FromEnhancedNavigation(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
        }

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"remove-counter-link-{i}"));
            Browser.DoesNotExist(By.Id($"remove-counter-link-{i}"));
            AssertBrowserLogContainsMessage($"Counter {i} was disposed");
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_CanGetDisposed_FromStreamingRenderingUpdate(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        // Components don't become interactive during streaming rendering, so we need to
        // add then via enhanced navigation first
        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
        }

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"remove-counter-link-{i}"));
            Browser.DoesNotExist(By.Id($"remove-counter-link-{i}"));
            AssertBrowserLogContainsMessage($"Counter {i} was disposed");
        }

        Browser.Click(By.Id("stop-streaming-link"));

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void DynamicallyAddedSsrComponents_CanGetRemoved_BeforeStreamingRenderingCompletes(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Exists(By.Id($"remove-counter-link-{i}"));
        }

        Browser.Click(By.Id($"remove-counter-link-0"));

        Browser.Click(By.Id("stop-streaming-link"));

        for (var i = 1; i < addCounterLinkIds.Length; i++)
        {
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorWebAssembly_AfterAddingWebAssemblyRootComponent()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorWebAssembly_WhenAddingWebAssemblyComponentAfterServerWasPreviouslyUsed()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        UnblockWebAssemblyResourceLoad();

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-2")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorServerOnFirstLoad_ThenWebAssemblyOnSuccessiveLoads()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        UnblockWebAssemblyResourceLoad();

        // Add a WebAssembly component to ensure the WebAssembly runtime
        // will be cached after we refresh the page.
        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id($"remove-counter-link-1"));
        Browser.DoesNotExist(By.Id("is-interactive-1"));

        Browser.Navigate().Refresh();

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Navigate().Refresh();

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorServer_IfBootResourceHashChanges()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        UnblockWebAssemblyResourceLoad();
        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id($"remove-counter-link-1"));
        Browser.DoesNotExist(By.Id("is-interactive-1"));

        Browser.Navigate().Refresh();

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        ForceWebAssemblyResourceCacheMiss("dummy hash");
        Browser.Navigate().Refresh();

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorWebAssembly_WhenAutoAndWebAssemblyComponentsAreAddedAtTheSameTime()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        ForceWebAssemblyResourceCacheMiss();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id("stop-streaming-link"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);
    }

    [Fact]
    public void AutoRenderMode_CanUseBlazorServer_WhenMultipleAutoComponentsAreAddedAtTheSameTime()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id("stop-streaming-link"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-1")).Text);
    }

    [Fact]
    public void AutoRenderMode_CanUseBlazorWebAssembly_WhenMultipleAutoComponentsAreAddedAtTheSameTime()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        ForceWebAssemblyResourceCacheMiss();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        // We start by adding a WebAssembly component to ensure the WebAssembly runtime
        // will be cached after we refresh the page.
        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id($"remove-counter-link-0"));
        Browser.DoesNotExist(By.Id("is-interactive-0"));

        Browser.Navigate().Refresh();

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-2")).Text);

        Browser.Click(By.Id("stop-streaming-link"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-2")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorWebAssembly_WhenBothServerAndWebAssemblyComponentsExist()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-2")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorServer_WhenOnlyServerComponentsExist_EvenAfterWebAssemblyResourcesLoad()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        ForceWebAssemblyResourceCacheMiss();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        // We start by adding a WebAssembly component to ensure the WebAssembly runtime
        // will be cached after we refresh the page.
        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id($"remove-counter-link-0"));
        Browser.DoesNotExist(By.Id("is-interactive-0"));

        Browser.Navigate().Refresh();

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        // Verify that Auto mode will use Blazor Server, even though the WebAssembly runtime is cached
        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-2")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorServer_AfterWebAssemblyComponentsNoLongerExist_ButServerComponentsDo()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-2")).Text);

        // Remove all WebAssembly components
        Browser.Click(By.Id("remove-counter-link-0"));
        Browser.Click(By.Id("remove-counter-link-2"));

        // Verify that Blazor Server gets used
        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-3")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-3")).Text);
    }

    [Fact]
    public void WebAssemblyRenderMode_DownloadsWebAssemblyResourcesInParallel()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity?ClearSiteData=True");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.True(() => GetMaxParallelWebAssemblyResourceDownloadCount() > 1);
    }

    [Fact]
    public void AutoRenderMode_DoesNotDownloadWebAssemblyResourcesInParallel()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity?ClearSiteData=True");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Equal(1, GetMaxParallelWebAssemblyResourceDownloadCount);
    }

    [Fact]
    public void Circuit_ShutsDown_WhenAllBlazorServerComponentsGetRemoved()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-0")).Text);

        Browser.Click(By.Id("remove-counter-link-0"));

        AssertBrowserLogContainsMessage("Connection disconnected.");
        AssertBrowserLogDoesNotContainErrors();
    }

    [Fact]
    public void Circuit_CanShutDownAndReInitializeMultipleTimes()
    {
        const int numCircuits = 5;

        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < numCircuits; i++)
        {
            Browser.Click(By.Id(AddServerPrerenderedId));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            Browser.Click(By.Id($"remove-counter-link-{i}"));

            AssertBrowserLogContainsMessage("Connection disconnected.");
            AssertBrowserLogDoesNotContainErrors();
            ClearBrowserLogs();
        }
    }

    [Fact]
    public async Task ReconnectionHandler_DoesNotRun_AfterIntentionallyShuttingDownTheCircuit()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-0")).Text);

        Browser.Click(By.Id("remove-counter-link-0"));

        AssertBrowserLogContainsMessage("Connection disconnected.");
        AssertBrowserLogDoesNotContainErrors();
        ClearBrowserLogs();

        // Wait for the reconnection handler to run, if it's going to. Hopefully it doesn't.
        await Task.Delay(5000);

        AssertBrowserLogDoesNotContainMessage("WebSocket connected to");
    }

    [Fact]
    public void DotNetObjectReference_CannotBeUsed_AfterCircuitShutsDown()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-0")).Text);

        // This JS call works via .NET object reference to a counter component.
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");
        Browser.Equal("1", () => Browser.FindElement(By.Id($"count-0")).Text);

        Browser.Click(By.Id($"remove-counter-link-0"));
        AssertBrowserLogContainsMessage("Connection disconnected.");
        AssertBrowserLogDoesNotContainErrors();

        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");

        AssertBrowserLogContainsMessage("Error: The circuit");
    }

    [Fact]
    public void DotNetObjectReference_CannotBeUsed_AfterCircuitShutsDown_AndANewCircuitIsInitialized()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-0")).Text);

        // This JS call works via .NET object reference to a counter component.
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");
        Browser.Equal("1", () => Browser.FindElement(By.Id($"count-0")).Text);

        Browser.Click(By.Id($"remove-counter-link-0"));
        AssertBrowserLogContainsMessage("Connection disconnected.");
        AssertBrowserLogDoesNotContainErrors();

        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");

        AssertBrowserLogContainsMessage("Error: The circuit");
        ClearBrowserLogs();

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-1")).Text);

        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(1)");
        Browser.Equal("1", () => Browser.FindElement(By.Id($"count-1")).Text);

        AssertBrowserLogDoesNotContainErrors();

        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");
        AssertBrowserLogContainsMessage("Error: The circuit");
    }

    [Fact]
    public void CanPersistPrerenderedState_Server()
    {
        Navigate($"{ServerPathBase}/persist-state?server=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("server")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedStateDeclaratively_Server()
    {
        Navigate($"{ServerPathBase}/persist-state?server=true&declarative=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("server")).Text);
        Browser.Equal("42", () => Browser.FindElement(By.Id("custom-server")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server")).Text);
        Browser.Equal("prerender-disabled-not-restored", () => Browser.FindElement(By.Id("prerendering-disabled-server")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_WebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-state?wasm=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("wasm")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedStateDeclaratively_WebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-state?wasm=true&declarative=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("wasm")).Text);
        Browser.Equal("42", () => Browser.FindElement(By.Id("custom-wasm")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm")).Text);
        Browser.Equal("prerender-disabled-not-restored", () => Browser.FindElement(By.Id("prerendering-disabled-wasm")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_Auto_PersistsOnWebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-state?auto=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("auto")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedStateDeclaratively_Auto_PersistsOnWebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-state?auto=true&declarative=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("auto")).Text);
        Browser.Equal("42", () => Browser.FindElement(By.Id("custom-auto")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto")).Text);
        Browser.Equal("prerender-disabled-not-restored", () => Browser.FindElement(By.Id("prerendering-disabled-auto")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_Auto_PersistsOnServer()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/persist-state?auto=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("auto")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-auto")).Text);
    }

    [Theory]
    [InlineData("server", "Server state", "Auto state", "not restored")]
    [InlineData("auto", "Server state", "Auto state", "not restored")]
    public void CanPersistPrerenderedState_ServicesState_PersistsOnServer(string mode, string expectedServerState, string expectedAutoState, string expectedWebAssemblyState)
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        if (mode == "auto")
        {
            BlockWebAssemblyResourceLoad();
        }

        Navigate($"{ServerPathBase}/persist-services-state?mode={mode}");
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode")).Text);
        Browser.Equal(expectedServerState, () => Browser.FindElement(By.Id("server-state")).Text);

        Browser.Equal(expectedAutoState, () => Browser.FindElement(By.Id("auto-state")).Text);
        Browser.Equal(expectedWebAssemblyState, () => Browser.FindElement(By.Id("wasm-state")).Text);

        Browser.Equal("not restored", () => Browser.FindElement(By.Id("filtered-server-state")).Text);
        Browser.Equal("not restored", () => Browser.FindElement(By.Id("filtered-auto-state")).Text);
        Browser.Equal("not restored", () => Browser.FindElement(By.Id("filtered-wasm-state")).Text);
    }

    [Theory]
    [InlineData("auto", "not restored", "Auto state", "WebAssembly state")]
    [InlineData("wasm", "not restored", "Auto state", "WebAssembly state")]
    public void CanPersistPrerenderedState_ServicesState_PersistsOnWasm(string mode, string expectedServerState, string expectedAutoState, string expectedWebAssemblyState)
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);

        Navigate($"{ServerPathBase}/persist-services-state?mode={mode}");

        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode")).Text);
        Browser.Equal(expectedServerState, () => Browser.FindElement(By.Id("server-state")).Text);
        Browser.Equal(expectedAutoState, () => Browser.FindElement(By.Id("auto-state")).Text);
        Browser.Equal(expectedWebAssemblyState, () => Browser.FindElement(By.Id("wasm-state")).Text);

        Browser.Equal("not restored", () => Browser.FindElement(By.Id("filtered-server-state")).Text);
        Browser.Equal("not restored", () => Browser.FindElement(By.Id("filtered-auto-state")).Text);
        Browser.Equal("not restored", () => Browser.FindElement(By.Id("filtered-wasm-state")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedStateDeclaratively_Auto_PersistsOnServer()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/persist-state?auto=true&declarative=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("auto")).Text);
        Browser.Equal("42", () => Browser.FindElement(By.Id("custom-auto")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-auto")).Text);
        Browser.Equal("prerender-disabled-not-restored", () => Browser.FindElement(By.Id("prerendering-disabled-auto")).Text);
    }

    [Fact]
    public void CanPersistState_AllRenderModesAtTheSameTime()
    {
        Navigate($"{ServerPathBase}/persist-state?server=true&wasm=true&auto=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("server")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server")).Text);

        Browser.Equal("restored", () => Browser.FindElement(By.Id("wasm")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm")).Text);

        Browser.Equal("restored", () => Browser.FindElement(By.Id("auto")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_ServerPrerenderedStateAvailableOnlyOnFirstRender()
    {
        Navigate($"{ServerPathBase}/persist-server-state");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("server")).Text);

        Browser.Click(By.Id("destroy-and-recreate"));

        Browser.Equal("not restored", () => Browser.FindElement(By.Id("server")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_WebAssemblyPrerenderedStateAvailableOnlyOnFirstRender()
    {
        Navigate($"{ServerPathBase}/persist-wasm-state");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("wasm")).Text);

        Browser.Click(By.Id("destroy-and-recreate"));

        Browser.Equal("not restored", () => Browser.FindElement(By.Id("wasm")).Text);
    }

    [Fact]
    public void NavigationManagerCanRefreshSSRPageWhenServerInteractivityEnabled()
    {
        Navigate($"{ServerPathBase}/forms/form-that-calls-navigation-manager-refresh");

        var guid = Browser.Exists(By.Id("guid")).Text;

        Browser.Exists(By.Id("submit-button")).Click();

        // Checking that the page was refreshed.
        // The redirect request method is GET.
        // Providing a Guid to check that it is not the initial GET request for the page
        Browser.NotEqual(guid, () => Browser.Exists(By.Id("guid")).Text);
        Browser.Equal("GET", () => Browser.Exists(By.Id("method")).Text);
    }

    [Fact]
    public void InteractiveServerRootComponent_CanAccessCircuitContext()
    {
        Navigate($"{ServerPathBase}/interactivity/circuit-context");
        CircuitContextTest.TestCircuitContextCore(Browser);
    }

    [Fact]
    public void InteractiveServerRootComponents_CanBecomeInteractive_WithoutInterferingWithOtherCircuits()
    {
        // Start by setting up 2 tabs with interactive server components.
        SetUpPageWithOneInteractiveServerComponent();

        var firstWindow = Browser.CurrentWindowHandle;
        Browser.SwitchTo().NewWindow(WindowType.Tab);
        var secondWindow = Browser.CurrentWindowHandle;

        SetUpPageWithOneInteractiveServerComponent();

        // Start streaming in the second tab.
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        // Add an interactive server component while streaming.
        // This will update the existing component, but the new component
        // won't become interactive until streaming ends.
        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id($"is-interactive-1")).Text);

        // Add an interactive server component in the first tab.
        // This component will become interactive immediately because the response
        // that rendered the component will have completed quickly.
        Browser.SwitchTo().Window(firstWindow);
        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-1")).Text);

        // Stop streaming in the second tab.
        // This will activate the pending component for interactivity.
        // This check verifies that a circuit can activate components from its most
        // recent response, even if that response isn't the most recent between all
        // circuits.
        Browser.SwitchTo().Window(secondWindow);
        Browser.Click(By.Id("stop-streaming-link"));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-1")).Text);

        void SetUpPageWithOneInteractiveServerComponent()
        {
            Navigate($"{ServerPathBase}/streaming-interactivity");

            Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

            Browser.Click(By.Id(AddServerPrerenderedId));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-0")).Text);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CanPerformNavigateToFromInteractiveEventHandler(bool suppressEnhancedNavigation, bool forceLoad)
    {
        EnhancedNavigationTestUtil.SuppressEnhancedNavigation(this, suppressEnhancedNavigation);

        // Get to the test page
        Navigate($"{ServerPathBase}/interactivity/navigateto");
        Browser.Equal("Interactive NavigateTo", () => Browser.FindElement(By.TagName("h1")).Text);
        var originalNavElem = Browser.FindElement(By.TagName("nav"));

        // Perform the navigation
        Browser.Click(By.Id(forceLoad ? "perform-navigateto-force" : "perform-navigateto"));
        Browser.True(() => Browser.Url.EndsWith("/nav", StringComparison.Ordinal));
        Browser.Equal("Hello", () => Browser.FindElement(By.Id("nav-home")).Text);

        // Verify the elements were preserved if and only if they should be
        var shouldPreserveElements = !suppressEnhancedNavigation && !forceLoad;
        Assert.Equal(shouldPreserveElements, !originalNavElem.IsStale());
    }

    private void BlockWebAssemblyResourceLoad()
    {
        // Force a WebAssembly resource cache miss so that we can fall back to using server interactivity
        ForceWebAssemblyResourceCacheMiss();

        ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('block-load-boot-resource', 'true')");

        // Clear caches so that we can block the resource load
        ((IJavaScriptExecutor)Browser).ExecuteScript("caches.keys().then(keys => keys.forEach(key => caches.delete(key)))");
    }

    private void UnblockWebAssemblyResourceLoad()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.unblockLoadBootResource()");
    }

    private void ForceWebAssemblyResourceCacheMiss(string resourceHash = null)
    {
        if (resourceHash is not null)
        {
            ((IJavaScriptExecutor)Browser).ExecuteScript($"localStorage.setItem('blazor-resource-hash:Components.WasmMinimal', '{resourceHash}')");
        }
        else
        {
            // Clear local storage so that the resource hash is not found
            ((IJavaScriptExecutor)Browser).ExecuteScript("localStorage.clear()");
        }
    }

    private long GetMaxParallelWebAssemblyResourceDownloadCount()
    {
        return (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window['__aspnetcore__testing__max__parallel__resource__download__count'] || 0;");
    }

    private string InteractiveCallsiteUrl(bool prerender, int? serverIncrement = default, int? webAssemblyIncrement = default)
    {
        var result = $"{ServerPathBase}/interactive-callsite?suppress-autostart&prerender={prerender}";

        if (serverIncrement.HasValue)
        {
            result += $"&server={serverIncrement}";
        }

        if (webAssemblyIncrement.HasValue)
        {
            result += $"&wasm={webAssemblyIncrement}";
        }

        return result;
    }

    private void AssertBrowserLogContainsMessage(string message)
        => Browser.True(() => DoesBrowserLogContainMessage(message));

    private void AssertBrowserLogDoesNotContainMessage(string message)
        => Browser.False(() => DoesBrowserLogContainMessage(message));

    private bool DoesBrowserLogContainMessage(string message)
    {
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        return entries.Any(entry => entry.Message.Contains(message));
    }

    private void AssertBrowserLogDoesNotContainErrors(Func<LogEntry, bool> filter = null)
    {
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        if (filter != null)
        {
            entries = entries.Where(filter).ToArray().AsReadOnly();
        }
        Assert.DoesNotContain(entries, entry => entry.Level == LogLevel.Severe);
    }

    private void ClearBrowserLogs()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("console.clear()");
    }

    [Fact]
    public void CanPersistMultiplePrerenderedStateDeclaratively_Server()
    {
        Navigate($"{ServerPathBase}/persist-multiple-state-declaratively?server=true");

        Browser.Equal("restored 1", () => Browser.FindElement(By.Id("server-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server-1")).Text);

        Browser.Equal("restored 2", () => Browser.FindElement(By.Id("server-2")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server-2")).Text);
    }

    [Fact]
    public void CanPersistMultiplePrerenderedStateDeclaratively_WebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-multiple-state-declaratively?wasm=true");

        Browser.Equal("restored 1", () => Browser.FindElement(By.Id("wasm-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm-1")).Text);

        Browser.Equal("restored 2", () => Browser.FindElement(By.Id("wasm-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm-2")).Text);
    }

    [Fact]
    public void CanPersistMultiplePrerenderedStateDeclaratively_Auto_PersistsOnServer()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/persist-multiple-state-declaratively?auto=true");

        Browser.Equal("restored 1", () => Browser.FindElement(By.Id("auto-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-auto-1")).Text);

        Browser.Equal("restored 2", () => Browser.FindElement(By.Id("auto-2")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-auto-2")).Text);
    }

    [Fact]
    public void CanPersistMultiplePrerenderedStateDeclaratively_Auto_PersistsOnWebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-multiple-state-declaratively?auto=true");

        Browser.Equal("restored 1", () => Browser.FindElement(By.Id("auto-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto-1")).Text);

        Browser.Equal("restored 2", () => Browser.FindElement(By.Id("auto-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto-2")).Text);
    }

    [Fact]
    public void CanPersistMultipleRootPrerenderedStateDeclaratively_WebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-multiple-root-component-state-declaratively?wasm=true");

        Browser.Equal("restored 1", () => Browser.FindElement(By.Id("wasm-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm-1")).Text);

        Browser.Equal("restored 2", () => Browser.FindElement(By.Id("wasm-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm-2")).Text);
    }

    [Fact]
    public void CanPersistMultipleRootPrerenderedStateDeclaratively_Server()
    {
        Navigate($"{ServerPathBase}/persist-multiple-root-component-state-declaratively?server=true");

        Browser.Equal("restored 1", () => Browser.FindElement(By.Id("server-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server-1")).Text);

        Browser.Equal("restored 2", () => Browser.FindElement(By.Id("server-2")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server-2")).Text);
    }

    [Fact]
    public void CanPersistMultipleRootPrerenderedStateDeclaratively_Auto_PersistsOnServer()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/persist-multiple-root-component-state-declaratively?auto=true");

        Browser.Equal("restored 1", () => Browser.FindElement(By.Id("auto-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-auto-1")).Text);

        Browser.Equal("restored 2", () => Browser.FindElement(By.Id("auto-2")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-auto-2")).Text);
    }

    [Fact]
    public void CanPersistMultipleRootPrerenderedStateDeclaratively_Auto_PersistsOnWebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-multiple-root-component-state-declaratively?auto=true");

        Browser.Equal("restored 1", () => Browser.FindElement(By.Id("auto-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto-1")).Text);

        Browser.Equal("restored 2", () => Browser.FindElement(By.Id("auto-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto-2")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NavigatesWithInteractivityByRequestRedirection(bool controlFlowByException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(!controlFlowByException);
        Navigate($"{ServerPathBase}/routing/ssr-navigate-to");
        Browser.Equal("Click submit to navigate to home", () => Browser.Exists(By.Id("test-info")).Text);
        Browser.Click(By.Id("redirectButton"));
        Browser.Equal("Routing test cases", () => Browser.Exists(By.Id("test-info")).Text);
    }

    [Theory]
    // prerendering (SSR) is tested in NoInteractivityTest
    [InlineData("ServerNonPrerendered")]
    [InlineData("WebAssemblyNonPrerendered")]
    public void ProgrammaticNavigationToNotExistingPathReExecutesTo404(string renderMode)
    {
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found?renderMode={renderMode}&navigate-programmatically=true");
        Assert404ReExecuted();
    }

    [Theory]
    // prerendering (SSR) is tested in NoInteractivityTest
    [InlineData("ServerNonPrerendered")]
    [InlineData("WebAssemblyNonPrerendered")]
    public void LinkNavigationToNotExistingPathReExecutesTo404(string renderMode)
    {
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found?renderMode={renderMode}");
        Browser.Click(By.Id("link-to-not-existing-page"));
        Assert404ReExecuted();
    }

    [Theory]
    // prerendering (SSR) is tested in NoInteractivityTest
    [InlineData("ServerNonPrerendered")]
    [InlineData("WebAssemblyNonPrerendered")]
    public void BrowserNavigationToNotExistingPathReExecutesTo404(string renderMode)
    {
        // non-existing path has to have re-execution middleware set up
        // so it has to have "reexecution" prefix. Otherwise middleware mapping
        // will not be activated, see configuration in Startup
        Navigate($"{ServerPathBase}/reexecution/not-existing-page?renderMode={renderMode}");
        Assert404ReExecuted();
    }

    [Fact]
    public void BrowserNavigationToNotExistingPathReExecutesTo404_Interactive()
    {
        // non-existing path has to have re-execution middleware set up
        // so it has to have "interactive-reexecution" prefix. Otherwise middleware mapping
        // will not be activated, see configuration in Startup
        Navigate($"{ServerPathBase}/interactive-reexecution/not-existing-page");
        Assert404ReExecuted();
        AssertReExecutedPageIsInteractive();
    }

    [Fact]
    public void PersistedState_IsEmitted_OnReExecutedPage()
    {
        Navigate($"{ServerPathBase}/interactive-reexecution/not-existing-page");

        Assert404ReExecuted();
        AssertReExecutedPageIsInteractive();
        Browser.Equal("restored", () => Browser.FindElement(By.Id("server")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server")).Text);
    }

    private void AssertReExecutedPageIsInteractive()
    {
        Browser.Equal("Current count: 0", () => Browser.FindElement(By.CssSelector("[role='status']")).Text);
        Browser.Click(By.Id("increment-button"));
        Browser.Equal("Current count: 1", () => Browser.FindElement(By.CssSelector("[role='status']")).Text);
    }

    private void Assert404ReExecuted() =>
        Browser.Equal("Welcome On Page Re-executed After Not Found Event", () => Browser.Exists(By.Id("test-info")).Text);
}
