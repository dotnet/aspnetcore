// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Layouts;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    // Integration tests for the end-to-end of successful Razor compilation of component definitions
    // Includes running the component code to verify the output.
    public class RenderingRazorIntegrationTest : RazorIntegrationTestBase
    {
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
                frame => AssertFrame.Whitespace(frame, 1),
                frame => AssertFrame.Whitespace(frame, 2), // @((object)null)
                frame => AssertFrame.Whitespace(frame, 3),
                frame => AssertFrame.Text(frame, "123", 4),
                frame => AssertFrame.Whitespace(frame, 5),
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
                @functions {
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
        public void SupportsElements()
        {
            // Arrange/Act
            var component = CompileToComponent("<myelem>Hello</myelem>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "myelem", 2, 0),
                frame => AssertFrame.Text(frame, "Hello", 1));
        }

        [Fact]
        public void SupportsSelfClosingElements()
        {
            // Arrange/Act
            var component = CompileToComponent("Some text so elem isn't at position 0 <myelem />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Text(frame, "Some text so elem isn't at position 0 ", 0),
                frame => AssertFrame.Element(frame, "myelem", 1, 1));
        }

        [Fact]
        public void SupportsVoidHtmlElements()
        {
            // Arrange/Act
            var component = CompileToComponent("Some text so elem isn't at position 0 <img>");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Text(frame, "Some text so elem isn't at position 0 ", 0),
                frame => AssertFrame.Element(frame, "img", 1, 1));
        }

        [Fact]
        public void SupportsComments()
        {
            // Arrange/Act
            var component = CompileToComponent("Start<!-- My comment -->End");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Start", 0),
                frame => AssertFrame.Text(frame, "End", 1));
        }

        [Fact]
        public void SupportsAttributesWithLiteralValues()
        {
            // Arrange/Act
            var component = CompileToComponent("<elem attrib-one=\"Value 1\" a2='v2' />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 3, 0),
                frame => AssertFrame.Attribute(frame, "attrib-one", "Value 1", 1),
                frame => AssertFrame.Attribute(frame, "a2", "v2", 2));
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

        // This test exercises the case where two IntermediateTokens are part of the same expression.
        // In these case they are split by a comment.
        [Fact]
        public void SupportsAttributesWithInterpolatedStringExpressionValues_SplitByComment()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"world\"; var myNum=123; }"
                + "<elem attr=\"Hello, @myValue.ToUpperInvariant()    with number @(myN@* Blazor is Blawesome! *@um*2)!\" />");

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
        public void SupportsDataDashAttributesWithLiteralValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "<elem data-abc=\"Hello\" />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "data-abc", "Hello", 1));
        }

        [Fact(Skip = "Currently broken due to #219. TODO: Once the issue is fixed, re-enable this test, remove the test below, and remove the implementation of its workaround.")]
        public void SupportsDataDashAttributesWithCSharpExpressionValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"My string\"; }"
                + "<elem data-abc=@myValue />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "data-abc", "My string", 1));
        }

        [Fact]
        public void TemporaryWorkaround_ConvertsDataUnderscoreAttributesToDataDash()
        {
            // This is a temporary workaround for https://github.com/aspnet/Blazor/issues/219
            //
            // Currently Razor's HtmlMarkupParser looks for data-* attributes and handles
            // them differently: https://github.com/aspnet/Razor/blob/dev/src/Microsoft.AspNetCore.Razor.Language/Legacy/HtmlMarkupParser.cs#L934
            // This is because Razor was historically used only on the server, and there's
            // an argument that data-* shouldn't support conditional server-side rendering
            // because of its HTML semantics. The result is that information about data-*
            // attributes isn't retained in the IR - all we get there is literal HTML
            // markup, which the Blazor code writer can't do anything useful with.
            //
            // The real solution would be to disable the parser's "data-*" special case
            // for Blazor. We don't yet have a mechanism for disabling it, so as a short
            // term workaround, we support data_* as an alternative syntax that renders
            // as data-* in the DOM.
            //
            // This workaround (the automatic conversion of data_* to data-*) will be removed
            // as soon as the underlying HTML parsing issue is resolved.

            // Arrange/Act
            var component = CompileToComponent(
                "@{ var myValue = \"My string\"; }"
                + "<elem data_abc=@myValue />");

            // Assert
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "data-abc", "My string", 1));
        }

        [Fact]
        public void SupportsAttributesWithEventHandlerValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<elem attr=@MyHandleEvent />
                @functions {
                    public bool HandlerWasCalled { get; set; } = false;

                    void MyHandleEvent(Microsoft.AspNetCore.Blazor.UIEventArgs eventArgs)
                    {
                        HandlerWasCalled = true;
                    }
                }");
            var handlerWasCalledProperty = component.GetType().GetProperty("HandlerWasCalled");

            // Assert
            Assert.False((bool)handlerWasCalledProperty.GetValue(component));
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame =>
                {
                    Assert.Equal(RenderTreeFrameType.Attribute, frame.FrameType);
                    Assert.Equal(1, frame.Sequence);
                    Assert.NotNull(frame.AttributeValue);

                    ((Action<UIEventArgs>)frame.AttributeValue)(null);
                    Assert.True((bool)handlerWasCalledProperty.GetValue(component));
                });
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
        public void SupportsTwoWayBindingForTextboxes()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input bind=""MyValue"" />
                @functions {
                    public string MyValue { get; set; } = ""Initial value"";
                }");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "Initial value", 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = "Modified value"
                    });
                    Assert.Equal("Modified value", myValueProperty.GetValue(component));
                });
        }

        [Fact]
        public void SupportsTwoWayBindingForTextareas()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<textarea bind=""MyValue"" ></textarea>
                @functions {
                    public string MyValue { get; set; } = ""Initial value"";
                }");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "textarea", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "Initial value", 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = "Modified value"
                    });
                    Assert.Equal("Modified value", myValueProperty.GetValue(component));
                });
        }

        [Fact]
        public void SupportsTwoWayBindingForDateValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input bind=""MyDate"" />
                @functions {
                    public DateTime MyDate { get; set; } = new DateTime(2018, 3, 4, 1, 2, 3);
                }");
            var myDateProperty = component.GetType().GetProperty("MyDate");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 3, 4, 1, 2, 3).ToString(), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    var newDateValue = new DateTime(2018, 3, 5, 4, 5, 6);
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = newDateValue.ToString()
                    });
                    Assert.Equal(newDateValue, myDateProperty.GetValue(component));
                });
        }

        [Fact]
        public void SupportsTwoWayBindingForDateValuesWithFormatString()
        {
            // Arrange/Act
            var testDateFormat = "ddd yyyy-MM-dd";
            var component = CompileToComponent(
                $@"<input bind=""@MyDate"" format-value=""{testDateFormat}"" />
                @functions {{
                    public DateTime MyDate {{ get; set; }} = new DateTime(2018, 3, 4);
                }}");
            var myDateProperty = component.GetType().GetProperty("MyDate");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", new DateTime(2018, 3, 4).ToString(testDateFormat), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = new DateTime(2018, 3, 5).ToString(testDateFormat)
                    });
                    Assert.Equal(new DateTime(2018, 3, 5), myDateProperty.GetValue(component));
                });
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
                frame => AssertFrame.Element(frame, "button", 2, 0),
                frame => AssertFrame.Attribute(frame, "onclick", "function(){console.log('hello');};", 1));
        }

        [Fact]
        public void SupportsEventHandlerWithLambda()
        {
            // Arrange
            var component = CompileToComponent(@"
<button onclick=""@(x => Clicked = true)"" />
@functions {
    public bool Clicked { get; set; }
}");

            var clicked = component.GetType().GetProperty("Clicked");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "button", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onclick", 1);

                    var func = Assert.IsType<Action<UIMouseEventArgs>>(frame.AttributeValue);
                    Assert.False((bool)clicked.GetValue(component));

                    func(new UIMouseEventArgs());
                    Assert.True((bool)clicked.GetValue(component));
                });
        }

        [Fact]
        public void SupportsEventHandlerWithMethodGroup()
        {
            // Arrange
            var component = CompileToComponent(@"
<button onclick=""@OnClick"" />
@functions {
    public void OnClick(UIMouseEventArgs e) { Clicked = true; }
    public bool Clicked { get; set; }
}");

            var clicked = component.GetType().GetProperty("Clicked");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "button", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onclick", 1);

                    var func = Assert.IsType<Action<UIMouseEventArgs>>(frame.AttributeValue);
                    Assert.False((bool)clicked.GetValue(component));

                    func(new UIMouseEventArgs());
                    Assert.True((bool)clicked.GetValue(component));
                });
        }

        [Fact]
        public void SupportsTwoWayBindingForBoolValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input bind=""MyValue"" />
                @functions {
                    public bool MyValue { get; set; } = true;
                }");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", true, 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = false
                    });
                    Assert.False((bool)myValueProperty.GetValue(component));
                });
        }

        [Fact]
        public void SupportsTwoWayBindingForEnumValues()
        {
            // Arrange/Act
            var myEnumType = FullTypeName<MyEnum>();
            var component = CompileToComponent(
                $@"<input bind=""MyValue"" />
                @functions {{
                    public {myEnumType} MyValue {{ get; set; }} = {myEnumType}.{nameof(MyEnum.FirstValue)};
                }}");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", MyEnum.FirstValue.ToString(), 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((Action<UIEventArgs>)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = MyEnum.SecondValue.ToString()
                    });
                    Assert.Equal(MyEnum.SecondValue, (MyEnum)myValueProperty.GetValue(component));
                });
        }

        public enum MyEnum { FirstValue, SecondValue }
    }
}
