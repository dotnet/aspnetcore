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
                frame => AssertFrame.Text(frame, new object().ToString(), 6),
                frame => AssertFrame.Whitespace(frame, 7));
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
                frame => AssertFrame.Text(frame, "Third", 0),
                frame => AssertFrame.Whitespace(frame, 1),
                frame => AssertFrame.Whitespace(frame, 2));
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

                    ((UIEventHandler)frame.AttributeValue)(null);
                    Assert.True((bool)handlerWasCalledProperty.GetValue(component));
                },
                frame => AssertFrame.Whitespace(frame, 2));
        }

        [Fact]
        public void SupportsAttributesWithCSharpCodeBlockValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<elem attr=@{ DidInvokeCode = true; } />
                @functions {
                    public bool DidInvokeCode { get; set; } = false;
                }");
            var didInvokeCodeProperty = component.GetType().GetProperty("DidInvokeCode");
            var frames = GetRenderTree(component);

            // Assert
            Assert.False((bool)didInvokeCodeProperty.GetValue(component));
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame =>
                {
                    Assert.Equal(RenderTreeFrameType.Attribute, frame.FrameType);
                    Assert.NotNull(frame.AttributeValue);
                    Assert.Equal(1, frame.Sequence);

                    ((UIEventHandler)frame.AttributeValue)(null);
                    Assert.True((bool)didInvokeCodeProperty.GetValue(component));
                },
                frame => AssertFrame.Whitespace(frame, 2));
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
                frame => AssertFrame.Whitespace(frame, 0),
                frame => AssertFrame.Text(frame, typeof(List<string>).FullName, 1));
        }

        [Fact]
        public void SupportsAttributeFramesEvaluatedInline()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<elem @onclick(MyHandler) />
                @functions {
                    public bool DidInvokeCode { get; set; } = false;
                    void MyHandler()
                    {
                        DidInvokeCode = true;
                    }
                }");
            var didInvokeCodeProperty = component.GetType().GetProperty("DidInvokeCode");

            // Assert
            Assert.False((bool)didInvokeCodeProperty.GetValue(component));
            Assert.Collection(GetRenderTree(component),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame =>
                {
                    Assert.Equal(RenderTreeFrameType.Attribute, frame.FrameType);
                    Assert.NotNull(frame.AttributeValue);
                    Assert.Equal(1, frame.Sequence);

                    ((UIEventHandler)frame.AttributeValue)(null);
                    Assert.True((bool)didInvokeCodeProperty.GetValue(component));
                },
                frame => AssertFrame.Whitespace(frame, 2));
        }

        [Fact]
        public void SupportsTwoWayBindingForTextboxes()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input @bind(MyValue) />
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
                    ((UIEventHandler)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = "Modified value"
                    });
                    Assert.Equal("Modified value", myValueProperty.GetValue(component));
                },
                frame => AssertFrame.Text(frame, "\n", 3));
        }

        [Fact]
        public void SupportsTwoWayBindingForDateValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input @bind(MyDate) />
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
                    ((UIEventHandler)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = newDateValue.ToString()
                    });
                    Assert.Equal(newDateValue, myDateProperty.GetValue(component));
                },
                frame => AssertFrame.Text(frame, "\n", 3));
        }

        [Fact]
        public void SupportsTwoWayBindingForDateValuesWithFormatString()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input @bind(MyDate, ""ddd yyyy-MM-dd"") />
                @functions {
                    public DateTime MyDate { get; set; } = new DateTime(2018, 3, 4);
                }");
            var myDateProperty = component.GetType().GetProperty("MyDate");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "Sun 2018-03-04", 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((UIEventHandler)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = "Mon 2018-03-05"
                    });
                    Assert.Equal(new DateTime(2018, 3, 5), myDateProperty.GetValue(component));
                },
                frame => AssertFrame.Text(frame, "\n", 3));
        }

        [Fact]
        public void SupportsTwoWayBindingForBoolValues()
        {
            // Arrange/Act
            var component = CompileToComponent(
                @"<input @bind(MyValue) />
                @functions {
                    public bool MyValue { get; set; } = true;
                }");
            var myValueProperty = component.GetType().GetProperty("MyValue");

            // Assert
            var frames = GetRenderTree(component);
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "input", 3, 0),
                frame => AssertFrame.Attribute(frame, "value", "True", 1),
                frame =>
                {
                    AssertFrame.Attribute(frame, "onchange", 2);

                    // Trigger the change event to show it updates the property
                    ((UIEventHandler)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = false
                    });
                    Assert.False((bool)myValueProperty.GetValue(component));
                },
                frame => AssertFrame.Text(frame, "\n", 3));
        }

        [Fact]
        public void SupportsTwoWayBindingForEnumValues()
        {
            // Arrange/Act
            var myEnumType = FullTypeName<MyEnum>();
            var component = CompileToComponent(
                $@"<input @bind(MyValue) />
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
                    ((UIEventHandler)frame.AttributeValue)(new UIChangeEventArgs
                    {
                        Value = MyEnum.SecondValue.ToString()
                    });
                    Assert.Equal(MyEnum.SecondValue, (MyEnum)myValueProperty.GetValue(component));
                },
                frame => AssertFrame.Text(frame, "\n", 3));
        }

        public enum MyEnum { FirstValue, SecondValue }
    }
}
