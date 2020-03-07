// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BasicTestApp;
using BasicTestApp.HierarchicalImportsTest.Subdir;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class ComponentRenderingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public ComponentRenderingTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        }

        [Fact]
        public void BasicTestAppCanBeServed()
        {
            Assert.Equal("Basic test app", Browser.Title);
        }

        [Fact]
        public void CanRenderTextOnlyComponent()
        {
            var appElement = Browser.MountTestComponent<TextOnlyComponent>();
            Assert.Equal("Hello from TextOnlyComponent", appElement.Text);
        }

        // This verifies that we've correctly configured the Razor language version via MSBuild.
        // See #974
        [Fact]
        public void CanRenderComponentWithDataDash()
        {
            var appElement = Browser.MountTestComponent<DataDashComponent>();
            var element = appElement.FindElement(By.Id("cool_beans"));
            Assert.Equal("17", element.GetAttribute("data-tab"));
            Assert.Equal("17", element.Text);
        }

        [Fact]
        public void CanRenderComponentWithAttributes()
        {
            var appElement = Browser.MountTestComponent<RedTextComponent>();
            var styledElement = appElement.FindElement(By.TagName("h1"));
            Assert.Equal("Hello, world!", styledElement.Text);
            Assert.Equal("color: red;", styledElement.GetAttribute("style"));
            Assert.Equal("somevalue", styledElement.GetAttribute("customattribute"));
        }

        [Fact]
        public void CanTriggerEvents()
        {
            // Initial count is zero
            var appElement = Browser.MountTestComponent<CounterComponent>();
            var countDisplayElement = appElement.FindElement(By.TagName("p"));
            Assert.Equal("Current count: 0", countDisplayElement.Text);

            // Clicking button increments count
            appElement.FindElement(By.TagName("button")).Click();
            Browser.Equal("Current count: 1", () => countDisplayElement.Text);
        }

        [Fact]
        public void CanTriggerAsyncEventHandlers()
        {
            // Initial state is stopped
            var appElement = Browser.MountTestComponent<AsyncEventHandlerComponent>();
            var stateElement = appElement.FindElement(By.Id("state"));
            Assert.Equal("Stopped", stateElement.Text);

            // Clicking 'tick' changes the state, and starts a task
            appElement.FindElement(By.Id("tick")).Click();
            Browser.Equal("Started", () => stateElement.Text);

            // Clicking 'tock' completes the task, which updates the state
            appElement.FindElement(By.Id("tock")).Click();
            Browser.Equal("Stopped", () => stateElement.Text);
        }

        [Fact]
        public void CanTriggerKeyPressEvents()
        {
            // List is initially empty
            var appElement = Browser.MountTestComponent<KeyPressEventComponent>();
            var inputElement = appElement.FindElement(By.TagName("input"));
            Func<IEnumerable<IWebElement>> liElements =
                () => appElement.FindElements(By.TagName("li"));
            Assert.Empty(liElements());

            // Typing adds element
            inputElement.SendKeys("a");
            Browser.Collection(liElements,
                li => Assert.Equal("a", li.Text));

            // Typing again adds another element
            inputElement.SendKeys("b");
            Browser.Collection(liElements,
                li => Assert.Equal("a", li.Text),
                li => Assert.Equal("b", li.Text));

            // Textbox contains typed text
            Assert.Equal("ab", inputElement.GetAttribute("value"));
        }

        [Fact]
        public void CanAddAndRemoveEventHandlersDynamically()
        {
            var appElement = Browser.MountTestComponent<CounterComponent>();
            var countDisplayElement = appElement.FindElement(By.TagName("p"));
            var incrementButton = appElement.FindElement(By.TagName("button"));
            var toggleClickHandlerCheckbox = appElement.FindElement(By.CssSelector("[type=checkbox]"));

            // Initial count is zero; clicking button increments count
            Assert.Equal("Current count: 0", countDisplayElement.Text);
            incrementButton.Click();
            Browser.Equal("Current count: 1", () => countDisplayElement.Text);

            // We can remove an event handler
            toggleClickHandlerCheckbox.Click();
            Browser.Empty(() => appElement.FindElements(By.Id("listening-message")));
            incrementButton.Click();
            Browser.Equal("Current count: 1", () => countDisplayElement.Text);

            // We can add an event handler
            toggleClickHandlerCheckbox.Click();
            appElement.FindElement(By.Id("listening-message"));
            incrementButton.Click();
            Browser.Equal("Current count: 2", () => countDisplayElement.Text);
        }

        [Fact]
        public void CanRenderChildComponents()
        {
            var appElement = Browser.MountTestComponent<ParentChildComponent>();
            Assert.Equal("Parent component",
                appElement.FindElement(By.CssSelector("fieldset > legend")).Text);

            var styledElement = appElement.FindElement(By.CssSelector("fieldset > h1"));
            Assert.Equal("Hello, world!", styledElement.Text);
            Assert.Equal("color: red;", styledElement.GetAttribute("style"));
            Assert.Equal("somevalue", styledElement.GetAttribute("customattribute"));
        }

        // Verifies we can render HTML content as a single block
        [Fact]
        public void CanRenderChildContent_StaticHtmlBlock()
        {
            var appElement = Browser.MountTestComponent<HtmlBlockChildContent>();
            Assert.Equal("<p>Some-Static-Text</p>",
                appElement.FindElement(By.Id("foo")).GetAttribute("innerHTML"));
        }

        // Verifies we can rewite more complex HTML content into blocks
        [Fact]
        public void CanRenderChildContent_MixedHtmlBlock()
        {
            var appElement = Browser.MountTestComponent<HtmlMixedChildContent>();

            var one = appElement.FindElement(By.Id("one"));
            Assert.Equal("<p>Some-Static-Text</p>", one.GetAttribute("innerHTML"));

            var two = appElement.FindElement(By.Id("two"));
            Assert.Equal("<span>More-Static-Text</span>", two.GetAttribute("innerHTML"));

            var three = appElement.FindElement(By.Id("three"));
            Assert.Equal("Some-Dynamic-Text", three.GetAttribute("innerHTML"));

            var four = appElement.FindElement(By.Id("four"));
            Assert.Equal("But this is static", four.GetAttribute("innerHTML"));
        }

        // Verifies we can rewrite HTML blocks with encoded HTML
        [Fact]
        public void CanRenderChildContent_EncodedHtmlInBlock()
        {
            var appElement = Browser.MountTestComponent<HtmlEncodedChildContent>();

            var one = appElement.FindElement(By.Id("one"));
            Assert.Equal("<p>Some-Static-Text</p>", one.GetAttribute("innerHTML"));

            var two = appElement.FindElement(By.Id("two"));
            Assert.Equal("&lt;span&gt;More-Static-Text&lt;/span&gt;", two.GetAttribute("innerHTML"));

            var three = appElement.FindElement(By.Id("three"));
            Assert.Equal("Some-Dynamic-Text", three.GetAttribute("innerHTML"));

            var four = appElement.FindElement(By.Id("four"));
            Assert.Equal("But this is static", four.GetAttribute("innerHTML"));
        }

        [Fact]
        public void CanTriggerEventsOnChildComponents()
        {
            // Counter is displayed as child component. Initial count is zero.
            var appElement = Browser.MountTestComponent<CounterComponentWrapper>();
            var counterDisplay = appElement
                .FindElements(By.TagName("p"))
                .Single(element => element.Text == "Current count: 0");

            // Clicking increments count in child component
            appElement.FindElement(By.TagName("button")).Click();
            Browser.Equal("Current count: 1", () => counterDisplay.Text);
        }

        [Fact]
        public void ChildComponentsRerenderWhenPropertiesChanged()
        {
            // Count value is displayed in child component with initial value zero
            var appElement = Browser.MountTestComponent<CounterComponentUsingChild>();
            var wholeCounterElement = appElement.FindElement(By.TagName("p"));
            var messageElementInChild = wholeCounterElement.FindElement(By.ClassName("message"));
            Assert.Equal("Current count: 0", wholeCounterElement.Text);
            Assert.Equal("0", messageElementInChild.Text);

            // Clicking increments count in child element
            appElement.FindElement(By.TagName("button")).Click();
            Browser.Equal("1", () => messageElementInChild.Text);
        }

        [Fact]
        public void CanAddAndRemoveChildComponentsDynamically()
        {
            // Initially there are zero child components
            var appElement = Browser.MountTestComponent<AddRemoveChildComponents>();
            var addButton = appElement.FindElement(By.ClassName("addChild"));
            var removeButton = appElement.FindElement(By.ClassName("removeChild"));
            Func<IEnumerable<IWebElement>> childComponentWrappers = () => appElement.FindElements(By.TagName("p"));
            Assert.Empty(childComponentWrappers());

            // Click to add/remove some child components
            addButton.Click();
            Browser.Collection(childComponentWrappers,
                elem => Assert.Equal("Child 1", elem.FindElement(By.ClassName("message")).Text));

            addButton.Click();
            Browser.Collection(childComponentWrappers,
                elem => Assert.Equal("Child 1", elem.FindElement(By.ClassName("message")).Text),
                elem => Assert.Equal("Child 2", elem.FindElement(By.ClassName("message")).Text));

            removeButton.Click();
            Browser.Collection(childComponentWrappers,
                elem => Assert.Equal("Child 1", elem.FindElement(By.ClassName("message")).Text));

            addButton.Click();
            Browser.Collection(childComponentWrappers,
                elem => Assert.Equal("Child 1", elem.FindElement(By.ClassName("message")).Text),
                elem => Assert.Equal("Child 3", elem.FindElement(By.ClassName("message")).Text));
        }

        [Fact]
        public void ChildComponentsNotifiedWhenPropertiesChanged()
        {
            // Child component receives notification that lets it compute a property before first render
            var appElement = Browser.MountTestComponent<PropertiesChangedHandlerParent>();
            var suppliedValueElement = appElement.FindElement(By.ClassName("supplied"));
            var computedValueElement = appElement.FindElement(By.ClassName("computed"));
            var incrementButton = appElement.FindElement(By.TagName("button"));
            Assert.Equal("You supplied: 100", suppliedValueElement.Text);
            Assert.Equal("I computed: 200", computedValueElement.Text);

            // When property changes, child is renotified before rerender
            incrementButton.Click();
            Browser.Equal("You supplied: 101", () => suppliedValueElement.Text);
            Assert.Equal("I computed: 202", computedValueElement.Text);
        }

        [Fact]
        public void CanRenderFragmentsWhilePreservingSurroundingElements()
        {
            // Initially, the region isn't shown
            var appElement = Browser.MountTestComponent<RenderFragmentToggler>();
            var originalButton = appElement.FindElement(By.TagName("button"));
            Func<IEnumerable<IWebElement>> fragmentElements = () => appElement.FindElements(By.CssSelector("p[name=fragment-element]"));
            Assert.Empty(fragmentElements());

            // The JS-side DOM builder handles regions correctly, placing elements
            // after the region after the corresponding elements
            Assert.Equal("The end", appElement.FindElements(By.CssSelector("div > *:last-child")).Single().Text);

            // When we click the button, the region is shown
            originalButton.Click();
            Browser.Single(fragmentElements);

            // The button itself was preserved, so we can click it again and see the effect
            originalButton.Click();
            Browser.Empty(fragmentElements);
        }

        [Fact]
        public void CanUseViewImportsHierarchically()
        {
            // The component is able to compile and output these type names only because
            // of the _ViewImports.cshtml files at the same and ancestor levels
            var appElement = Browser.MountTestComponent<ComponentUsingImports>();
            Assert.Collection(appElement.FindElements(By.TagName("p")),
                elem => Assert.Equal(typeof(Complex).FullName, elem.Text),
                elem => Assert.Equal(typeof(AssemblyHashAlgorithm).FullName, elem.Text));
        }

        [Fact]
        public void CanUseComponentAndStaticContentFromExternalNuGetPackage()
        {
            var appElement = Browser.MountTestComponent<ExternalContentPackage>();

            // NuGet packages can use JS interop features to provide
            // .NET code access to browser APIs
            var showPromptButton = appElement.FindElements(By.TagName("button")).First();
            showPromptButton.Click();

            var modal = new WebDriverWait(Browser, TimeSpan.FromSeconds(3))
                .Until(SwitchToAlert);
            modal.SendKeys("Some value from test");
            modal.Accept();
            var promptResult = appElement.FindElement(By.TagName("strong"));
            Browser.Equal("Some value from test", () => promptResult.Text);

            // NuGet packages can also embed entire components (themselves
            // authored as Razor files), including static content. The CSS value
            // here is in a .css file, so if it's correct we know that static content
            // file was loaded.
            var specialStyleDiv = appElement.FindElement(By.ClassName("special-style"));
            Assert.Equal("50px", specialStyleDiv.GetCssValue("padding"));

            // The external components are fully functional, not just static HTML
            var externalComponentButton = specialStyleDiv.FindElement(By.TagName("button"));
            Assert.Equal("Click me", externalComponentButton.Text);
            externalComponentButton.Click();
            Browser.Equal("It works", () => externalComponentButton.Text);
        }

        [Fact]
        public void CanRenderSvgWithCorrectNamespace()
        {
            var appElement = Browser.MountTestComponent<SvgComponent>();

            var svgElement = appElement.FindElement(By.XPath("//*[local-name()='svg' and namespace-uri()='http://www.w3.org/2000/svg']"));
            Assert.NotNull(svgElement);

            var svgCircleElement = appElement.FindElement(By.XPath("//*[local-name()='circle' and namespace-uri()='http://www.w3.org/2000/svg']"));
            Assert.NotNull(svgCircleElement);
            Assert.Equal("10", svgCircleElement.GetAttribute("r"));

            appElement.FindElement(By.TagName("button")).Click();
            Browser.Equal("20", () => svgCircleElement.GetAttribute("r"));
        }

        [Fact]
        public void CanRenderSvgChildComponentWithCorrectNamespace()
        {
            var appElement = Browser.MountTestComponent<SvgWithChildComponent>();

            var svgElement = appElement.FindElement(By.XPath("//*[local-name()='svg' and namespace-uri()='http://www.w3.org/2000/svg']"));
            Assert.NotNull(svgElement);

            var svgCircleElement = appElement.FindElement(By.XPath("//*[local-name()='circle' and namespace-uri()='http://www.w3.org/2000/svg']"));
            Assert.NotNull(svgCircleElement);
        }

        [Fact]
        public void LogicalElementInsertionWorksHierarchically()
        {
            var appElement = Browser.MountTestComponent<LogicalElementInsertionCases>();
            Browser.Equal("First Second Third", () => appElement.Text);
        }

        [Fact]
        public void CanUseJsInteropToReferenceElements()
        {
            var appElement = Browser.MountTestComponent<ElementRefComponent>();
            var inputElement = appElement.FindElement(By.Id("capturedElement"));
            var buttonElement = appElement.FindElement(By.TagName("button"));

            Assert.Equal(string.Empty, inputElement.GetAttribute("value"));

            buttonElement.Click();
            Browser.Equal("Clicks: 1", () => inputElement.GetAttribute("value"));
            buttonElement.Click();
            Browser.Equal("Clicks: 2", () => inputElement.GetAttribute("value"));
        }

        [Fact]
        public void CanCaptureReferencesToDynamicallyAddedElements()
        {
            var appElement = Browser.MountTestComponent<ElementRefComponent>();
            var buttonElement = appElement.FindElement(By.TagName("button"));
            var checkbox = appElement.FindElement(By.CssSelector("input[type=checkbox]"));

            // We're going to remove the input. But first, put in some contents
            // so we can observe it's not the same instance later
            appElement.FindElement(By.Id("capturedElement")).SendKeys("some text");

            // Remove the captured element
            checkbox.Click();
            Browser.Empty(() => appElement.FindElements(By.Id("capturedElement")));

            // Re-add it; observe it starts empty again
            checkbox.Click();
            var inputElement = appElement.FindElement(By.Id("capturedElement"));
            Assert.Equal(string.Empty, inputElement.GetAttribute("value"));

            // See that the capture variable was automatically updated to reference the new instance
            buttonElement.Click();
            Browser.Equal("Clicks: 1", () => inputElement.GetAttribute("value"));
        }

        [Fact]
        public void CanCaptureReferencesToDynamicallyAddedComponents()
        {
            var appElement = Browser.MountTestComponent<ComponentRefComponent>();
            var incrementButtonSelector = By.CssSelector("#child-component button");
            var currentCountTextSelector = By.CssSelector("#child-component p:first-of-type");
            var resetButton = appElement.FindElement(By.Id("reset-child"));
            var toggleChildCheckbox = appElement.FindElement(By.Id("toggle-child"));
            Func<string> currentCountText = () => appElement.FindElement(currentCountTextSelector).Text;

            // Verify the reference was captured initially
            appElement.FindElement(incrementButtonSelector).Click();
            Browser.Equal("Current count: 1", currentCountText);
            resetButton.Click();
            Browser.Equal("Current count: 0", currentCountText);
            appElement.FindElement(incrementButtonSelector).Click();
            Browser.Equal("Current count: 1", currentCountText);

            // Remove and re-add a new instance of the child, checking the text was reset
            toggleChildCheckbox.Click();
            Browser.Empty(() => appElement.FindElements(incrementButtonSelector));
            toggleChildCheckbox.Click();
            Browser.Equal("Current count: 0", currentCountText);

            // Verify we have a new working reference
            appElement.FindElement(incrementButtonSelector).Click();
            Browser.Equal("Current count: 1", currentCountText);
            resetButton.Click();
            Browser.Equal("Current count: 0", currentCountText);
        }

        [Fact]
        public void CanUseJsInteropForRefElementsDuringOnAfterRender()
        {
            var appElement = Browser.MountTestComponent<AfterRenderInteropComponent>();
            Browser.Equal("Value set after render", () => Browser.FindElement(By.TagName("input")).GetAttribute("value"));
        }

        [Fact]
        public void CanRenderMarkupBlocks()
        {
            var appElement = Browser.MountTestComponent<MarkupBlockComponent>();

            // Static markup
            Assert.Equal(
                "attributes",
                appElement.FindElement(By.CssSelector("p span#attribute-example")).Text);

            // Dynamic markup (from a custom RenderFragment)
            Assert.Equal(
                "[Here is an example. We support multiple-top-level nodes.]",
                appElement.FindElement(By.Id("dynamic-markup-block")).Text);
            Assert.Equal(
                "example",
                appElement.FindElement(By.CssSelector("#dynamic-markup-block strong#dynamic-element em")).Text);

            // Dynamic markup (from a MarkupString)
            Assert.Equal(
                "This is a markup string.",
                appElement.FindElement(By.ClassName("markup-string-value")).Text);
            Assert.Equal(
                "markup string",
                appElement.FindElement(By.CssSelector(".markup-string-value em")).Text);

            // Updating markup blocks
            appElement.FindElement(By.TagName("button")).Click();
            Browser.Equal(
                "[The output was changed completely.]",
                () => appElement.FindElement(By.Id("dynamic-markup-block")).Text);
            Assert.Equal(
                "changed",
                appElement.FindElement(By.CssSelector("#dynamic-markup-block span em")).Text);
        }

        [Fact]
        public void CanRenderRazorTemplates()
        {
            var appElement = Browser.MountTestComponent<RazorTemplates>();

            // code block template (component parameter)
            var element = appElement.FindElement(By.CssSelector("div#codeblocktemplate ol"));
            Assert.Collection(
                element.FindElements(By.TagName("li")),
                e => Assert.Equal("#1 - a", e.Text),
                e => Assert.Equal("#2 - b", e.Text),
                e => Assert.Equal("#3 - c", e.Text));
        }

        [Fact]
        public void CanRenderMultipleChildContent()
        {
            var appElement = Browser.MountTestComponent<MultipleChildContent>();

            var table = appElement.FindElement(By.TagName("table"));

            var thead = table.FindElement(By.TagName("thead"));
            Assert.Collection(
                thead.FindElements(By.TagName("th")),
                e => Assert.Equal("Col1", e.Text),
                e => Assert.Equal("Col2", e.Text),
                e => Assert.Equal("Col3", e.Text));

            var tfoot = table.FindElement(By.TagName("tfoot"));
            Assert.Empty(tfoot.FindElements(By.TagName("td")));

            var toggle = appElement.FindElement(By.Id("toggle"));
            toggle.Click();

            Browser.Collection(
                () => tfoot.FindElements(By.TagName("td")),
                e => Assert.Equal("The", e.Text),
                e => Assert.Equal("", e.Text),
                e => Assert.Equal("End", e.Text));
        }

        [Fact]
        public async Task CanAcceptSimultaneousRenderRequests()
        {
            var expectedOutput = string.Join(
                string.Empty,
                Enumerable.Range(0, 100).Select(_ => "😊"));

            var appElement = Browser.MountTestComponent<ConcurrentRenderParent>();

            // It's supposed to pause the rendering for this long. The WaitAssert below
            // allows it to take up extra time if needed.
            await Task.Delay(1000);

            var outputElement = appElement.FindElement(By.Id("concurrent-render-output"));
            Browser.Equal(expectedOutput, () => outputElement.Text);
        }

        [Fact]
        public void CanDispatchRenderToSyncContext()
        {
            var appElement = Browser.MountTestComponent<DispatchingComponent>();
            var result = appElement.FindElement(By.Id("result"));

            appElement.FindElement(By.Id("run-with-dispatch")).Click();

            Browser.Equal("Success (completed synchronously)", () => result.Text);
        }

        [Fact]
        public void CanDoubleDispatchRenderToSyncContext()
        {
            var appElement = Browser.MountTestComponent<DispatchingComponent>();
            var result = appElement.FindElement(By.Id("result"));

            appElement.FindElement(By.Id("run-with-double-dispatch")).Click();

            Browser.Equal("Success (completed synchronously)", () => result.Text);
        }

        [Fact]
        public virtual void CanDispatchAsyncWorkToSyncContext()
        {
            var appElement = Browser.MountTestComponent<DispatchingComponent>();
            var result = appElement.FindElement(By.Id("result"));

            appElement.FindElement(By.Id("run-async-with-dispatch")).Click();

            Browser.Equal("First Second Third Fourth Fifth", () => result.Text);
        }

        [Fact]
        public void CanPerformInteropImmediatelyOnComponentInsertion()
        {
            var appElement = Browser.MountTestComponent<InteropOnInitializationComponent>();
            Browser.Equal("Hello from interop call", () => appElement.FindElement(By.Id("val-get-by-interop")).Text);
            Browser.Equal("Hello from interop call", () => appElement.FindElement(By.Id("val-set-by-interop")).GetAttribute("value"));
        }

        [Fact]
        public void CanUseAddMultipleAttributes()
        {
            var appElement = Browser.MountTestComponent<DuplicateAttributesComponent>();

            var selector = By.CssSelector("#duplicate-on-element > div");
            Browser.Exists(selector);

            var element = appElement.FindElement(selector);
            Assert.Equal(string.Empty, element.GetAttribute("bool")); // attribute is present
            Assert.Equal("middle-value", element.GetAttribute("string"));
            Assert.Equal("unmatched-value", element.GetAttribute("unmatched"));

            selector = By.CssSelector("#duplicate-on-element-override > div");
            element = appElement.FindElement(selector);
            Assert.Null(element.GetAttribute("bool")); // attribute is not present
            Assert.Equal("other-text", element.GetAttribute("string"));
            Assert.Equal("unmatched-value", element.GetAttribute("unmatched"));
        }

        [Fact]
        public void CanPatchRenderTreeToMatchLatestDOMState()
        {
            var appElement = Browser.MountTestComponent<MovingCheckboxesComponent>();
            var incompleteItemsSelector = By.CssSelector(".incomplete-items li");
            var completeItemsSelector = By.CssSelector(".complete-items li");
            Browser.Exists(incompleteItemsSelector);

            // Mark first item as done; observe the remaining incomplete item appears unchecked
            // because the diff algorithm explicitly unchecks it
            appElement.FindElement(By.CssSelector(".incomplete-items .item-isdone")).Click();
            Browser.True(() =>
            {
                var incompleteLIs = appElement.FindElements(incompleteItemsSelector);
                return incompleteLIs.Count == 1
                    && !incompleteLIs[0].FindElement(By.CssSelector(".item-isdone")).Selected;
            });

            // Mark first done item as not done; observe the remaining complete item appears checked
            // because the diff algorithm explicitly re-checks it
            appElement.FindElement(By.CssSelector(".complete-items .item-isdone")).Click();
            Browser.True(() =>
            {
                var completeLIs = appElement.FindElements(completeItemsSelector);
                return completeLIs.Count == 2
                    && completeLIs[0].FindElement(By.CssSelector(".item-isdone")).Selected;
            });
        }

        static IAlert SwitchToAlert(IWebDriver driver)
        {
            try
            {
                return driver.SwitchTo().Alert();
            }
            catch (NoAlertPresentException)
            {
                return null;
            }
        }
    }
}
