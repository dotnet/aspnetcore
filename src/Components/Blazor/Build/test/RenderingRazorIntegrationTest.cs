// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    // Integration tests for the end-to-end of successful Razor compilation of component definitions
    // Includes running the component code to verify the output.
    public class RenderingRazorIntegrationTest : RazorIntegrationTestBase
    {
        public RenderingRazorIntegrationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void SupportsPlainText()
        {
            // Arrange/Act
            var component = CompileToComponent("Some plain text");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Some plain text", 0));
        }

        [Fact]
        public void SupportsCSharpExpressions()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
                @(""Hello"")
                @((object)null)
                @(123)
                @(new object())
            ");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Hello", 0),
                frame => AssertFrame.MarkupWhitespace(frame, 1),
                frame => AssertFrame.TextWhitespace(frame, 2), // @((object)null)
                frame => AssertFrame.MarkupWhitespace(frame, 3),
                frame => AssertFrame.Text(frame, "123", 4),
                frame => AssertFrame.MarkupWhitespace(frame, 5),
                frame => AssertFrame.Text(frame, new object().ToString(), 6));
        }

        [Fact]
        public void SupportsCSharpFunctionsBlock()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
                @foreach(var item in items) {
                    @item
                }
                @code {
                    string[] items = new[] { ""First"", ""Second"", ""Third"" };
                }
            ");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "First", 0),
                frame => AssertFrame.Text(frame, "Second", 0),
                frame => AssertFrame.Text(frame, "Third", 0));
        }

        [Fact]
        public void SupportsElementsWithDynamicContent()
        {
            // Arrange/Act
            var component = CompileToComponent("<myelem>Hello @(\"there\")</myelem>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "myelem", 3, 0),
                frame => AssertFrame.Text(frame, "Hello ", 1),
                frame => AssertFrame.Text(frame, "there", 2));
        }

        [Fact]
        public void SupportsElementsAsStaticBlock()
        {
            // Arrange/Act
            var component = CompileToComponent("<myelem>Hello</myelem>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Markup(frame, "<myelem>Hello</myelem>", 0));
        }

        [Fact]
        public void CreatesSeparateMarkupFrameForEachTopLevelStaticElement()
        {
            // The JavaScript-side rendering code does not rely on this behavior. It supports
            // inserting markup frames with arbitrary markup (e.g., multiple top-level elements
            // or none). This test exists only as an observation of the current behavior rather
            // than a promise that we never want to change it.

            // Arrange/Act
            var component = CompileToComponent(
                "<root>@(\"Hi\") <child1>a</child1> <child2><another>b</another></child2> </root>");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "root", 5, 0),
                frame => AssertFrame.Text(frame, "Hi", 1),
                frame => AssertFrame.Text(frame, " ", 2),
                frame => AssertFrame.Markup(frame, "<child1>a</child1> ", 3),
                frame => AssertFrame.Markup(frame, "<child2><another>b</another></child2> ", 4));
        }

        [Fact]
        public void RendersMarkupStringAsMarkupFrame()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var someMarkup = new MarkupString(\"<div>Hello</div>\"); }"
                + "<p>@someMarkup</p>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "p", 2, 0),
                frame => AssertFrame.Markup(frame, "<div>Hello</div>", 1));
        }

        [Fact]
        public void SupportsSelfClosingElementsWithDynamicContent()
        {
            // Arrange/Act
            var component = CompileToComponent("Some text so elem isn't at position 0 <myelem myattr=@(\"val\") />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Text(frame, "Some text so elem isn't at position 0 ", 0),
                frame => AssertFrame.Element(frame, "myelem", 2, 1),
                frame => AssertFrame.Attribute(frame, "myattr", "val", 2));
        }

        [Fact]
        public void SupportsSelfClosingElementsAsStaticBlock()
        {
            // Arrange/Act
            var component = CompileToComponent("Some text so elem isn't at position 0 <input attr='123' />");

            // Assert
            Assert.Collection(
                GetRenderTree(component),
                frame => AssertFrame.Markup(frame, "Some text so elem isn't at position 0 <input attr=\"123\">", 0));
        }

        [Fact]
        public void SupportsVoidHtmlElements()
        {
            // Arrange/Act
            var component = CompileToComponent("Some text so elem isn't at position 0 <img>");

            // Assert
            Assert.Collection(
                GetRenderTree(component),
                frame => AssertFrame.Markup(frame, "Some text so elem isn't at position 0 <img>", 0));
        }

        [Fact]
        public void SupportsComments()
        {
            // Arrange/Act
            var component = CompileToComponent("Start<!-- My comment -->End");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Markup(frame, "StartEnd", 0));
        }

        [Fact]
        public void SupportsAttributesWithLiteralValues()
        {
            // Arrange/Act
            var component = CompileToComponent("<elem attrib-one=\"Value 1\" a2='v2'>@(\"Hello\")</elem>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 4, 0),
                frame => AssertFrame.Attribute(frame, "attrib-one", "Value 1", 1),
                frame => AssertFrame.Attribute(frame, "a2", "v2", 2),
                frame => AssertFrame.Text(frame, "Hello", 3));
        }

        [Fact]
        public void SupportsAttributesWithStringExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"My string\"; }"
                + "<elem attr=@myValue />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "My string", 1));
        }

        [Fact]
        public void SupportsAttributesWithNonStringExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = 123; }"
                + "<elem attr=@myValue />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "123", 1));
        }

        [Fact]
        public void SupportsAttributesWithInterpolatedStringExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"world\"; var myNum=123; }"
                + "<elem attr=\"Hello, @myValue.ToUpperInvariant()    with number @(myNum*2)!\" />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "Hello, WORLD    with number 246!", 1));
        }

        [Fact]
        public void SupportsAttributesWithInterpolatedTernaryExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"world\"; }"
                + "<elem attr=\"Hello, @(true ? myValue : \"nothing\")!\" />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "Hello, world!", 1));
        }

        [Fact]
        public void SupportsHyphenedAttributesWithCSharpExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"My string\"; }"
                + "<elem abc-def=@myValue />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "abc-def", "My string", 1));
        }

        [Fact]
        public void SupportsDataDashAttributes()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
@{
  var myValue = ""Expression value"";
}
<elem data-abc=""Literal value"" data-def=""@myValue"" />");

            // Assert
            Assert.Collection(
                GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 3, 0),
                frame => AssertFrame.Attribute(frame, "data-abc", "Literal value", 1),
                frame => AssertFrame.Attribute(frame, "data-def", "Expression value", 2));
        }

        [Fact]
        public void SupportsUsingStatements()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"@using System.Collections.Generic
                @(typeof(List<string>).FullName)");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, typeof(List<string>).FullName, 0));
        }

        [Fact]
        public async Task SupportsTwoWayBindingForTextboxes()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<input @bind=""MyValue"" />
@code {
    public string MyValue { get; set; } = ""Initial value"";
}");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            var renderer = new TestRenderer();

            // Assert
            EventCallback setter = default;
            var frames = GetRenderTree(renderer, component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "Initial value", 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);
                    setter = Assert.IsType<EventCallback>(frame.AttributeValue);
                });

            // Trigger the change event to show it updates the property
            //
            // This should always complete synchronously.
            var task = renderer.Dispatcher.InvokeAsync(() => setter.InvokeAsync(new ChangeEventArgs { Value = "Modified value", }));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            await task;

            Assert.Equal("Modified value", myValueProperty.GetValue(component));
        }

        [Fact]
        public async Task SupportsTwoWayBindingForTextareas()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<textarea @bind=""MyValue"" ></textarea>
@code {
    public string MyValue { get; set; } = ""Initial value"";
}");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            var renderer = new TestRenderer();

            // Assert
            EventCallback setter = default;
            var frames = GetRenderTree(renderer, component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "textarea", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "Initial value", 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);
                    setter = Assert.IsType<EventCallback>(frame.AttributeValue);
                });

            // Trigger the change event to show it updates the property
            //
            // This should always complete synchronously.
            var task = renderer.Dispatcher.InvokeAsync(() => setter.InvokeAsync(new ChangeEventArgs { Value = "Modified value", }));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            await task;

            Assert.Equal("Modified value", myValueProperty.GetValue(component));
        }

        [Fact]
        public async Task SupportsTwoWayBindingForDateValues()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<input @bind=""MyDate"" />
@code {
    public DateTime MyDate { get; set; } = new DateTime(2018, 3, 4, 1, 2, 3);
}");
            var myDateProperty = component.GetType().GetProperty("MyDate");

            var renderer = new TestRenderer();

            // Assert
            EventCallback setter = default;
            var frames = GetRenderTree(renderer, component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 3, 4, 1, 2, 3).ToString(), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);
                    setter = Assert.IsType<EventCallback>(frame.AttributeValue);
                });

            // Trigger the change event to show it updates the property
            // Trigger the change event to show it updates the property
            //
            // This should always complete synchronously.
            var newDateValue = new DateTime(2018, 3, 5, 4, 5, 6);
            var task = renderer.Dispatcher.InvokeAsync(() => setter.InvokeAsync(new ChangeEventArgs { Value = newDateValue.ToString(), }));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            await task;

            Assert.Equal(newDateValue, myDateProperty.GetValue(component));
        }

        [Fact]
        public async Task SupportsTwoWayBindingForDateValuesWithFormatString()
        {
            // Arrange/Act
            var testDateFormat = "ddd yyyy-MM-dd";
            var component = CompileToComponent($@"
@using Microsoft.AspNetCore.Components.Web
<input @bind=""@MyDate"" @bind:format=""{testDateFormat}"" />
@code {{
    public DateTime MyDate {{ get; set; }} = new DateTime(2018, 3, 4);
}}");
            var myDateProperty = component.GetType().GetProperty("MyDate");

            var renderer = new TestRenderer();

            // Assert
            EventCallback setter = default;
            var frames = GetRenderTree(renderer, component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 3, 4).ToString(testDateFormat), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);
                    setter = Assert.IsType<EventCallback>(frame.AttributeValue);
                });

            // Trigger the change event to show it updates the property
            //
            // This should always complete synchronously.
            var task = renderer.Dispatcher.InvokeAsync(() => setter.InvokeAsync(new ChangeEventArgs { Value = new DateTime(2018, 3, 5).ToString(testDateFormat), }));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            await task;

            Assert.Equal(new DateTime(2018, 3, 5), myDateProperty.GetValue(component));
        }

        [Fact] // In this case, onclick is just a normal HTML attribute
        public void SupportsEventHandlerWithString()
        {
            // Arrange
            var component = CompileToComponent(@"
<button onclick=""function(){console.log('hello');};"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Markup(frame, "<button onclick=\"function(){console.log('hello');};\"></button>", 0));
        }

        [Fact]
        public void SupportsEventHandlerWithLambda()
        {
            // Arrange
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<button @onclick=""x => Clicked = true"" />
@code {
    public bool Clicked { get; set; }
}");

            var clicked = component.GetType().GetProperty("Clicked");

            var renderer = new TestRenderer();

            // Act
            var frames = GetRenderTree(renderer, component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "button", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onclick", 1);

                    var func = Assert.IsType<Action<MouseEventArgs>>(frame.AttributeValue);
                    Assert.False((bool)clicked.GetValue(component));

                    func(new MouseEventArgs());
                    Assert.True((bool)clicked.GetValue(component));
                });
        }

        [Fact]
        public void SupportsEventHandlerWithMethodGroup()
        {
            // Arrange
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<button @onclick=""OnClick"" />
@code {
    public void OnClick(MouseEventArgs e) { Clicked = true; }
    public bool Clicked { get; set; }
}");

            var clicked = component.GetType().GetProperty("Clicked");

            var renderer = new TestRenderer();

            // Act
            var frames = GetRenderTree(renderer, component);

            // Assert
            Action<MouseEventArgs> func = default; // Since this is a method group, we don't need to create an EventCallback
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "button", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onclick", 1);

                    func = Assert.IsType<Action<MouseEventArgs>>(frame.AttributeValue);
                    Assert.False((bool)clicked.GetValue(component));


                });

            func.Invoke(new MouseEventArgs());
            Assert.True((bool)clicked.GetValue(component));
        }

        [Fact]
        public async Task SupportsTwoWayBindingForBoolValues()
        {
            // Arrange/Act
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<input @bind=""MyValue"" />
@code {
    public bool MyValue { get; set; } = true;
}");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            var renderer = new TestRenderer();

            // Assert
            EventCallback setter = default;
            var frames = GetRenderTree(renderer, component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", true, 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);
                    setter = Assert.IsType<EventCallback>(frame.AttributeValue);
                });

            // Trigger the change event to show it updates the property
            //
            // This should always complete synchronously.
            var task =  renderer.Dispatcher.InvokeAsync(() => setter.InvokeAsync(new ChangeEventArgs() { Value = false, }));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            await task;

            Assert.False((bool)myValueProperty.GetValue(component));
        }

        [Fact]
        public async Task SupportsTwoWayBindingForEnumValues()
        {
            // Arrange/Act
            var myEnumType = FullTypeName<MyEnum>();
            var component = CompileToComponent($@"
@using Microsoft.AspNetCore.Components.Web
<input @bind=""MyValue"" />
@code {{
    public {myEnumType} MyValue {{ get; set; }} = {myEnumType}.{nameof(MyEnum.FirstValue)};
}}");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            var renderer = new TestRenderer();

            // Assert
            EventCallback setter = default;
            var frames = GetRenderTree(renderer, component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", MyEnum.FirstValue.ToString(), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);
                    setter = Assert.IsType<EventCallback>(frame.AttributeValue);
                });

            // Trigger the change event to show it updates the property
            //
            // This should always complete synchronously.
            var task = renderer.Dispatcher.InvokeAsync(() => setter.InvokeAsync(new ChangeEventArgs { Value = MyEnum.SecondValue.ToString(), }));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            await task;

            Assert.Equal(MyEnum.SecondValue, (MyEnum)myValueProperty.GetValue(component));
        }

        public enum MyEnum { FirstValue, SecondValue }

        [Fact]
        public void RazorTemplate_NonGeneric_CanBeUsedFromRazorCode()
        {
            // Arrange
            var component = CompileToComponent(@"
@{ RenderFragment template = @<div>@(""Hello, World!"".ToLower())</div>; }
@for (var i = 0; i < 3; i++)
{
    @template;
}
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1));
        }

        [Fact]
        public void RazorTemplate_Generic_CanBeUsedFromRazorCode()
        {
            // Arrange
            var component = CompileToComponent(@"
@{ RenderFragment<string> template = (context) => @<div>@context.ToLower()</div>; }
@for (var i = 0; i < 3; i++)
{
    @template(""Hello, World!"");
}
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1));
        }

        [Fact]
        public void RazorTemplate_NonGeneric_CanBeUsedFromMethod()
        {
            // Arrange
            var component = CompileToComponent(@"
@(Repeat(@<div>@(""Hello, World!"".ToLower())</div>, 3))

@code {
    RenderFragment Repeat(RenderFragment template, int count)
    {
        return (b) =>
        {
            for (var i = 0; i < count; i++)
            {
                b.AddContent(i, template);
            }
        };
    }
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            //
            // The sequence numbers start at 1 here because there is an AddContent(0, Repeat(....) call
            // that precedes the definition of the lambda. Sequence numbers for the lambda are allocated
            // from the same logical sequence as the surrounding code.
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2),
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2),
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2));
        }

        [Fact]
        public void RazorTemplate_Generic_CanBeUsedFromMethod()
        {
            // Arrange
            var component = CompileToComponent(@"
@(Repeat((context) => @<div>@context.ToLower()</div>, ""Hello, World!"", 3))

@code {
    RenderFragment Repeat<T>(RenderFragment<T> template, T value, int count)
    {
        return (b) =>
        {
            for (var i = 0; i < count; i++)
            {
                b.AddContent(i, template, value);
            }
        };
    }
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            //
            // The sequence numbers start at 1 here because there is an AddContent(0, Repeat(....) call
            // that precedes the definition of the lambda. Sequence numbers for the lambda are allocated
            // from the same logical sequence as the surrounding code.
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2),
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2),
                frame => AssertFrame.Element(frame, "div", 2, 1),
                frame => AssertFrame.Text(frame, "hello, world!", 2));
        }
    }
}
