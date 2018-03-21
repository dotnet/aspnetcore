// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class ComponentRenderingRazorIntegrationTest : RazorIntegrationTestBase
    {
        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void Render_ChildComponent_Simple()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent/>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 1, 0));
        }

        [Fact]
        public void Render_ChildComponent_WithParameters()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class SomeType
    {
    }

    public class MyComponent : BlazorComponent
    {
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public string StringProperty { get; set; }
        public SomeType ObjectProperty { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent 
    IntProperty=""123""
    BoolProperty=""true""
    StringProperty=""My string""
    ObjectProperty=""new SomeType()"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 5, 0),
                frame => AssertFrame.Attribute(frame, "IntProperty", 123, 1),
                frame => AssertFrame.Attribute(frame, "BoolProperty", true, 2),
                frame => AssertFrame.Attribute(frame, "StringProperty", "My string", 3),
                frame =>
                {
                    AssertFrame.Attribute(frame, "ObjectProperty", 4);
                    Assert.Equal("Test.SomeType", frame.AttributeValue.GetType().FullName);
                });
        }

        [Fact]
        public void Render_ChildComponent_WithExplicitStringParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public string StringProperty { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent StringProperty=""@(42.ToString())"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame => AssertFrame.Attribute(frame, "StringProperty", "42", 1));
        }

        [Fact]
        public void Render_ChildComponent_WithNonPropertyAttributes()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent, IComponent
    {
        void IComponent.SetParameters(ParameterCollection parameters)
        {
        }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent some-attribute=""foo"" another-attribute=""@(42.ToString())"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "some-attribute", "foo", 1),
                frame => AssertFrame.Attribute(frame, "another-attribute", "42", 2));
        }


        [Theory]
        [InlineData("e => Increment(e)")]
        [InlineData("(e) => Increment(e)")]
        [InlineData("@(e => Increment(e))")]
        [InlineData("@(e => { Increment(e); })")]
        [InlineData("Increment")]
        [InlineData("@Increment")]
        [InlineData("@(Increment)")]
        public void Render_ChildComponent_WithEventHandler(string expression)
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public UIEventHandler OnClick { get; set; }
    }
}
"));

            var component = CompileToComponent($@"
@addTagHelper *, TestAssembly
@using Microsoft.AspNetCore.Blazor
<MyComponent OnClick=""{expression}""/>

@functions {{
    private int counter;
    private void Increment(UIEventArgs e) {{
        counter++;
    }}
}}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "OnClick", 1);

                    // The handler will have been assigned to a lambda
                    var handler = Assert.IsType<UIEventHandler>(frame.AttributeValue);
                    Assert.Equal("Test.TestComponent", handler.Target.GetType().FullName);
                },
                frame => AssertFrame.Whitespace(frame, 2));
        }

        [Fact]
        public void Render_ChildComponent_WithExplicitEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public UIEventHandler OnClick { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
@using Microsoft.AspNetCore.Blazor
<MyComponent OnClick=""@Increment""/>

@functions {
    private int counter;
    private void Increment(UIEventArgs e) {
        counter++;
    }
}");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame =>
                {
                    AssertFrame.Attribute(frame, "OnClick", 1);

                    // The handler will have been assigned to a lambda
                    var handler = Assert.IsType<UIEventHandler>(frame.AttributeValue);
                    Assert.Equal("Test.TestComponent", handler.Target.GetType().FullName);
                    Assert.Equal("Increment", handler.Method.Name);
                },
                frame => AssertFrame.Whitespace(frame, 2));
        }

        [Fact]
        public void Render_ChildComponent_WithMinimizedBoolAttribute()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public bool BoolProperty { get; set; }
    }
}"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent BoolProperty />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame => AssertFrame.Attribute(frame, "BoolProperty", true, 1));
        }

        [Fact]
        public void Render_ChildComponent_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public string MyAttr { get; set; }

        public RenderFragment ChildContent { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent MyAttr=""abc"">Some text<some-child a='1'>Nested text</some-child></MyComponent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert: component frames are correct
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "MyAttr", "abc", 1),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 2));

            // Assert: Captured ChildContent frames are correct
            var childFrames = GetFrames((RenderFragment)frames[2].AttributeValue);
            Assert.Collection(
                childFrames,
                frame => AssertFrame.Text(frame, "Some text", 3),
                frame => AssertFrame.Element(frame, "some-child", 3, 4),
                frame => AssertFrame.Attribute(frame, "a", "1", 5),
                frame => AssertFrame.Text(frame, "Nested text", 6));
        }

        [Fact]
        public void Render_ChildComponent_Nested()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(CSharpSyntaxTree.ParseText(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        public RenderFragment ChildContent { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<MyComponent><MyComponent>Some text</MyComponent></MyComponent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert: outer component frames are correct
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 1));

            // Assert: first level of ChildContent is correct
            // Note that we don't really need the sequence numbers to continue on from the
            // sequence numbers at the parent level. All that really matters is that they are
            // correct relative to each other (i.e., incrementing) within the nesting level.
            // As an implementation detail, it happens that they do follow on from the parent
            // level, but we could change that part of the implementation if we wanted.
            var innerFrames = GetFrames((RenderFragment)frames[1].AttributeValue).ToArray();
            Assert.Collection(
                innerFrames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 2),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 3));

            // Assert: second level of ChildContent is correct
            Assert.Collection(
                GetFrames((RenderFragment)innerFrames[1].AttributeValue),
                frame => AssertFrame.Text(frame, "Some text", 4));
        }
    }
}
