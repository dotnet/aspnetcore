// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Numerics;
using BasicTestApp;
using BasicTestApp.HierarchicalImportsTest.Subdir;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class ComponentRenderingTest : BasicTestAppTestBase
    {
        public ComponentRenderingTest(
            BrowserFixture browserFixture,
            DevHostServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            Navigate(ServerPathBase, noReload: true);
        }

        [Fact]
        public void BasicTestAppCanBeServed()
        {
            Assert.Equal("Basic test app", Browser.Title);
        }

        [Fact]
        public void CanRenderTextOnlyComponent()
        {
            var appElement = MountTestComponent<TextOnlyComponent>();
            Assert.Equal("Hello from TextOnlyComponent", appElement.Text);
        }

        [Fact]
        public void CanRenderComponentWithAttributes()
        {
            var appElement = MountTestComponent<RedTextComponent>();
            var styledElement = appElement.FindElement(By.TagName("h1"));
            Assert.Equal("Hello, world!", styledElement.Text);
            Assert.Equal("color: red;", styledElement.GetAttribute("style"));
            Assert.Equal("somevalue", styledElement.GetAttribute("customattribute"));
        }

        [Fact]
        public void CanTriggerEvents()
        {
            // Initial count is zero
            var appElement = MountTestComponent<CounterComponent>();
            var countDisplayElement = appElement.FindElement(By.TagName("p"));
            Assert.Equal("Current count: 0", countDisplayElement.Text);

            // Clicking button increments count
            appElement.FindElement(By.TagName("button")).Click();
            Assert.Equal("Current count: 1", countDisplayElement.Text);
        }

        [Fact]
        public void CanTriggerAsyncEventHandlers()
        {
            // Initial state is stopped
            var appElement = MountTestComponent<AsyncEventHandlerComponent>();
            var stateElement = appElement.FindElement(By.Id("state"));
            Assert.Equal("Stopped", stateElement.Text);

            // Clicking 'tick' changes the state, and starts a task
            appElement.FindElement(By.Id("tick")).Click();
            Assert.Equal("Started", stateElement.Text);

            // Clicking 'tock' completes the task, which updates the state
            appElement.FindElement(By.Id("tock")).Click();
            Assert.Equal("Stopped", stateElement.Text);
        }

        [Fact]
        public void CanTriggerKeyPressEvents()
        {
            // List is initially empty
            var appElement = MountTestComponent<KeyPressEventComponent>();
            var inputElement = appElement.FindElement(By.TagName("input"));
            Func<IEnumerable<IWebElement>> liElements =
                () => appElement.FindElements(By.TagName("li"));
            Assert.Empty(liElements());

            // Typing adds element
            inputElement.SendKeys("a");
            Assert.Collection(liElements(),
                li => Assert.Equal("a", li.Text));

            // Typing again adds another element
            inputElement.SendKeys("b");
            Assert.Collection(liElements(),
                li => Assert.Equal("a", li.Text),
                li => Assert.Equal("b", li.Text));
        }

        [Fact]
        public void CanAddAndRemoveEventHandlersDynamically()
        {
            var appElement = MountTestComponent<CounterComponent>();
            var countDisplayElement = appElement.FindElement(By.TagName("p"));
            var incrementButton = appElement.FindElement(By.TagName("button"));
            var toggleClickHandlerCheckbox = appElement.FindElement(By.CssSelector("[type=checkbox]"));

            // Initial count is zero; clicking button increments count
            Assert.Equal("Current count: 0", countDisplayElement.Text);
            incrementButton.Click();
            Assert.Equal("Current count: 1", countDisplayElement.Text);

            // We can remove an event handler
            toggleClickHandlerCheckbox.Click();
            incrementButton.Click();
            Assert.Equal("Current count: 1", countDisplayElement.Text);

            // We can add an event handler
            toggleClickHandlerCheckbox.Click();
            incrementButton.Click();
            Assert.Equal("Current count: 2", countDisplayElement.Text);
        }

        [Fact]
        public void CanRenderChildComponents()
        {
            var appElement = MountTestComponent<ParentChildComponent>();
            Assert.Equal("Parent component",
                appElement.FindElement(By.CssSelector("fieldset > legend")).Text);

            var styledElement = appElement.FindElement(By.CssSelector("fieldset > h1"));
            Assert.Equal("Hello, world!", styledElement.Text);
            Assert.Equal("color: red;", styledElement.GetAttribute("style"));
            Assert.Equal("somevalue", styledElement.GetAttribute("customattribute"));
        }

        [Fact]
        public void CanTriggerEventsOnChildComponents()
        {
            // Counter is displayed as child component. Initial count is zero.
            var appElement = MountTestComponent<CounterComponentWrapper>();
            var counterDisplay = appElement
                .FindElements(By.TagName("p"))
                .Single(element => element.Text == "Current count: 0");

            // Clicking increments count in child component
            appElement.FindElement(By.TagName("button")).Click();
            Assert.Equal("Current count: 1", counterDisplay.Text);
        }

        [Fact]
        public void ChildComponentsRerenderWhenPropertiesChanged()
        {
            // Count value is displayed in child component with initial value zero
            var appElement = MountTestComponent<CounterComponentUsingChild>();
            var wholeCounterElement = appElement.FindElement(By.TagName("p"));
            var messageElementInChild = wholeCounterElement.FindElement(By.ClassName("message"));
            Assert.Equal("Current count: 0", wholeCounterElement.Text);
            Assert.Equal("0", messageElementInChild.Text);

            // Clicking increments count in child element
            appElement.FindElement(By.TagName("button")).Click();
            Assert.Equal("1", messageElementInChild.Text);
        }

        [Fact]
        public void CanAddAndRemoveChildComponentsDynamically()
        {
            // Initially there are zero child components
            var appElement = MountTestComponent<AddRemoveChildComponents>();
            var addButton = appElement.FindElement(By.ClassName("addChild"));
            var removeButton = appElement.FindElement(By.ClassName("removeChild"));
            Func<IEnumerable<IWebElement>> childComponentWrappers = () => appElement.FindElements(By.TagName("p"));
            Assert.Empty(childComponentWrappers());

            // Click to add some child components
            addButton.Click();
            addButton.Click();
            removeButton.Click();
            addButton.Click();
            Assert.Collection(childComponentWrappers(),
                elem => Assert.Equal("Child 1", elem.FindElement(By.ClassName("message")).Text),
                elem => Assert.Equal("Child 3", elem.FindElement(By.ClassName("message")).Text));
        }

        [Fact]
        public void ChildComponentsNotifiedWhenPropertiesChanged()
        {
            // Child component receives notification that lets it compute a property before first render
            var appElement = MountTestComponent<PropertiesChangedHandlerParent>();
            var suppliedValueElement = appElement.FindElement(By.ClassName("supplied"));
            var computedValueElement = appElement.FindElement(By.ClassName("computed"));
            var incrementButton = appElement.FindElement(By.TagName("button"));
            Assert.Equal("You supplied: 100", suppliedValueElement.Text);
            Assert.Equal("I computed: 200", computedValueElement.Text);

            // When property changes, child is renotified before rerender
            incrementButton.Click();
            Assert.Equal("You supplied: 101", suppliedValueElement.Text);
            Assert.Equal("I computed: 202", computedValueElement.Text);
        }

        [Fact]
        public void CanRenderFragmentsWhilePreservingSurroundingElements()
        {
            // Initially, the region isn't shown
            var appElement = MountTestComponent<RenderFragmentToggler>();
            var originalButton = appElement.FindElement(By.TagName("button"));
            var fragmentElements = appElement.FindElements(By.CssSelector("p[name=fragment-element]"));
            Assert.Empty(fragmentElements);

            // The JS-side DOM builder handles regions correctly, placing elements
            // after the region after the corresponding elements
            Assert.Equal("The end", appElement.FindElements(By.CssSelector("div > *:last-child")).Single().Text);

            // When we click the button, the region is shown
            originalButton.Click();
            fragmentElements = appElement.FindElements(By.CssSelector("p[name=fragment-element]"));
            Assert.Single(fragmentElements);

            // The button itself was preserved, so we can click it again and see the effect
            originalButton.Click();
            fragmentElements = appElement.FindElements(By.CssSelector("p[name=fragment-element]"));
            Assert.Empty(fragmentElements);
        }

        [Fact]
        public void CanUseViewImportsHierarchically()
        {
            // The component is able to compile and output these type names only because
            // of the _ViewImports.cshtml files at the same and ancestor levels
            var appElement = MountTestComponent<ComponentUsingImports>();
            Assert.Collection(appElement.FindElements(By.TagName("p")),
                elem => Assert.Equal(typeof(Complex).FullName, elem.Text),
                elem => Assert.Equal(typeof(AssemblyHashAlgorithm).FullName, elem.Text));
        }

        [Fact]
        public void CanUseComponentAndStaticContentFromExternalNuGetPackage()
        {
            var appElement = MountTestComponent<ExternalContentPackage>();

            // NuGet packages can use Blazor's JS interop features to provide
            // .NET code access to browser APIs
            var showPromptButton = appElement.FindElements(By.TagName("button")).First();
            showPromptButton.Click();
            var modal = Browser.SwitchTo().Alert();
            modal.SendKeys("Some value from test");
            modal.Accept();
            var promptResult = appElement.FindElement(By.TagName("strong"));
            Assert.Equal("Some value from test", promptResult.Text);

            // NuGet packages can also embed entire Blazor components (themselves
            // authored as Razor files), including static content. The CSS value
            // here is in a .css file, so if it's correct we know that static content
            // file was loaded.
            var specialStyleDiv = appElement.FindElement(By.ClassName("special-style"));
            Assert.Equal("50px", specialStyleDiv.GetCssValue("padding"));

            // The external Blazor components are fully functional, not just static HTML
            var externalComponentButton = specialStyleDiv.FindElement(By.TagName("button"));
            Assert.Equal("Click me", externalComponentButton.Text);
            externalComponentButton.Click();
            Assert.Equal("It works", externalComponentButton.Text);
        }

        [Fact]
        public void CanRenderSvgWithCorrectNamespace()
        {
            var appElement = MountTestComponent<SvgComponent>();

            var svgElement = appElement.FindElement(By.XPath("//*[local-name()='svg' and namespace-uri()='http://www.w3.org/2000/svg']"));
            Assert.NotNull(svgElement);

            var svgCircleElement = appElement.FindElement(By.XPath("//*[local-name()='circle' and namespace-uri()='http://www.w3.org/2000/svg']"));
            Assert.NotNull(svgCircleElement);
        }

        [Fact]
        public void CanRenderSvgChildComponentWithCorrectNamespace()
        {
            var appElement = MountTestComponent<SvgWithChildComponent>();

            var svgElement = appElement.FindElement(By.XPath("//*[local-name()='svg' and namespace-uri()='http://www.w3.org/2000/svg']"));
            Assert.NotNull(svgElement);

            var svgCircleElement = appElement.FindElement(By.XPath("//*[local-name()='circle' and namespace-uri()='http://www.w3.org/2000/svg']"));
            Assert.NotNull(svgCircleElement);
        }

        [Fact]
        public void LogicalElementInsertionWorksHierarchically()
        {
            var appElement = MountTestComponent<LogicalElementInsertionCases>();
            Assert.Equal("First Second Third", appElement.Text);
        }

        [Fact]
        public void CanUseJsInteropToReferenceElements()
        {
            var appElement = MountTestComponent<ElementRefComponent>();
            var inputElement = appElement.FindElement(By.Id("capturedElement"));
            var buttonElement = appElement.FindElement(By.TagName("button"));

            Assert.Equal(string.Empty, inputElement.GetAttribute("value"));

            buttonElement.Click();
            Assert.Equal("Clicks: 1", inputElement.GetAttribute("value"));
            buttonElement.Click();
            Assert.Equal("Clicks: 2", inputElement.GetAttribute("value"));
        }

        [Fact]
        public void CanCaptureReferencesToDynamicallyAddedElements()
        {
            var appElement = MountTestComponent<ElementRefComponent>();
            var buttonElement = appElement.FindElement(By.TagName("button"));
            var checkbox = appElement.FindElement(By.CssSelector("input[type=checkbox]"));

            // We're going to remove the input. But first, put in some contents
            // so we can observe it's not the same instance later
            appElement.FindElement(By.Id("capturedElement")).SendKeys("some text");

            // Remove the captured element
            checkbox.Click();
            Assert.Empty(appElement.FindElements(By.Id("capturedElement")));

            // Re-add it; observe it starts empty again
            checkbox.Click();
            var inputElement = appElement.FindElement(By.Id("capturedElement"));
            Assert.NotNull(inputElement);
            Assert.Equal(string.Empty, inputElement.GetAttribute("value"));

            // See that the capture variable was automatically updated to reference the new instance
            buttonElement.Click();
            Assert.Equal("Clicks: 1", inputElement.GetAttribute("value"));
        }

        [Fact]
        public void CanCaptureReferencesToDynamicallyAddedComponents()
        {
            var appElement = MountTestComponent<ComponentRefComponent>();
            var incrementButtonSelector = By.CssSelector("#child-component button");
            var currentCountTextSelector = By.CssSelector("#child-component p:first-of-type");
            var resetButton = appElement.FindElement(By.Id("reset-child"));
            var toggleChildCheckbox = appElement.FindElement(By.Id("toggle-child"));

            // Verify the reference was captured initially
            appElement.FindElement(incrementButtonSelector).Click();
            Assert.Equal("Current count: 1", appElement.FindElement(currentCountTextSelector).Text);
            resetButton.Click();
            Assert.Equal("Current count: 0", appElement.FindElement(currentCountTextSelector).Text);
            appElement.FindElement(incrementButtonSelector).Click();
            Assert.Equal("Current count: 1", appElement.FindElement(currentCountTextSelector).Text);

            // Remove and re-add a new instance of the child, checking the text was reset
            toggleChildCheckbox.Click();
            Assert.Empty(appElement.FindElements(incrementButtonSelector));
            toggleChildCheckbox.Click();
            Assert.Equal("Current count: 0", appElement.FindElement(currentCountTextSelector).Text);

            // Verify we have a new working reference
            appElement.FindElement(incrementButtonSelector).Click();
            Assert.Equal("Current count: 1", appElement.FindElement(currentCountTextSelector).Text);
            resetButton.Click();
            Assert.Equal("Current count: 0", appElement.FindElement(currentCountTextSelector).Text);
        }
    }
}
