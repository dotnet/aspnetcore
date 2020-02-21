// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class ComponentRenderingRazorIntegrationTest : RazorIntegrationTestBase
    {
        public ComponentRenderingRazorIntegrationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void Render_ChildComponent_Simple()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

            var component = CompileToComponent(@"
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
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SomeType
    {
    }

    public class MyComponent : ComponentBase
    {
        [Parameter] public int IntProperty { get; set; }
        [Parameter] public bool BoolProperty { get; set; }
        [Parameter] public string StringProperty { get; set; }
        [Parameter] public SomeType ObjectProperty { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
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
        public void Render_ChildComponent_TriesToSetNonParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        public int IntProperty { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
<MyComponent  IntProperty=""123"" />");

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => GetRenderTree(component));

            // Assert
            Assert.Equal(
                "Object of type 'Test.MyComponent' has a property matching the name 'IntProperty', " +
                    "but it does not have [ParameterAttribute] or [CascadingParameterAttribute] applied.",
                ex.Message);
        }

        [Fact]
        public void Render_ChildComponent_WithExplicitStringParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string StringProperty { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
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
            AdditionalSyntaxTrees.Add(Parse(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase, IComponent
    {
        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }
}
"));

            var component = CompileToComponent(@"
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
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public Action<MouseEventArgs> OnClick { get; set; }
    }
}
"));

            var component = CompileToComponent($@"
@using Microsoft.AspNetCore.Components.Web
<MyComponent OnClick=""{expression}""/>

@code {{
    private int counter;
    private void Increment(MouseEventArgs e) {{
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
                    var handler = Assert.IsType<Action<MouseEventArgs>>(frame.AttributeValue);
                    Assert.Equal("Test.TestComponent", handler.Target.GetType().FullName);
                });
        }

        [Fact]
        public void Render_ChildComponent_WithExplicitEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public Action<EventArgs> OnClick { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
<MyComponent OnClick=""@Increment""/>

@code {
    private int counter;
    private void Increment(EventArgs e) {
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
                    var handler = Assert.IsType<Action<EventArgs>>(frame.AttributeValue);
                    Assert.Equal("Test.TestComponent", handler.Target.GetType().FullName);
                    Assert.Equal("Increment", handler.Method.Name);
                });
        }

        [Fact]
        public void Render_ChildComponent_WithMinimizedBoolAttribute()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public bool BoolProperty { get; set; }
    }
}"));

            var component = CompileToComponent(@"
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
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public string MyAttr { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
<MyComponent MyAttr=""abc"">Some text<some-child a='1'>Nested text @(""Hello"")</some-child></MyComponent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert: component frames are correct
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 3, 0),
                frame => AssertFrame.Attribute(frame, "MyAttr", "abc", 1),
                frame => AssertFrame.Attribute(frame, "ChildContent", 2));

            // Assert: Captured ChildContent frames are correct
            var childFrames = GetFrames((RenderFragment)frames[2].AttributeValue);
            Assert.Collection(
                childFrames.AsEnumerable(),
                frame => AssertFrame.Text(frame, "Some text", 3),
                frame => AssertFrame.Element(frame, "some-child", 4, 4),
                frame => AssertFrame.Attribute(frame, "a", "1", 5),
                frame => AssertFrame.Text(frame, "Nested text ", 6),
                frame => AssertFrame.Text(frame, "Hello", 7));
        }

        [Fact]
        public void Render_ChildComponent_Nested()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
<MyComponent><MyComponent>Some text</MyComponent></MyComponent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert: outer component frames are correct
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 0),
                frame => AssertFrame.Attribute(frame, "ChildContent", 1));

            // Assert: first level of ChildContent is correct
            // Note that we don't really need the sequence numbers to continue on from the
            // sequence numbers at the parent level. All that really matters is that they are
            // correct relative to each other (i.e., incrementing) within the nesting level.
            // As an implementation detail, it happens that they do follow on from the parent
            // level, but we could change that part of the implementation if we wanted.
            var innerFrames = GetFrames((RenderFragment)frames[1].AttributeValue).AsEnumerable().ToArray();
            Assert.Collection(
                innerFrames,
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 2),
                frame => AssertFrame.Attribute(frame, "ChildContent", 3));

            // Assert: second level of ChildContent is correct
            Assert.Collection(
                GetFrames((RenderFragment)innerFrames[1].AttributeValue).AsEnumerable(),
                frame => AssertFrame.Text(frame, "Some text", 4));
        }

        [Fact] // https://github.com/dotnet/blazor/issues/773
        public void Regression_773()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class SurveyPrompt : ComponentBase
    {
        [Parameter] public string Title { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
@page ""/""

<SurveyPrompt Title=""<div>Test!</div>"" />
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.SurveyPrompt", 2, 0),
                frame => AssertFrame.Attribute(frame, "Title", "<div>Test!</div>", 1));
        }


        [Fact]
        public void Regression_784()
        {
            // Arrange

            // Act
            var component = CompileToComponent(@"
@using Microsoft.AspNetCore.Components.Web
<p @onmouseover=""OnComponentHover"" style=""background: @ParentBgColor;"" />
@code {
    public string ParentBgColor { get; set; } = ""#FFFFFF"";

    public void OnComponentHover(MouseEventArgs e)
    {
    }
}
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "p", 3, 0),
                frame => AssertFrame.Attribute(frame, "onmouseover", 1),
                frame => AssertFrame.Attribute(frame, "style", "background: #FFFFFF;", 2));
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/6185")]
        public void Render_Component_HtmlEncoded()
        {
            // Arrange
            var component = CompileToComponent(@"&lt;span&gt;Hi&lt;/span&gt;");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Text(frame, "<span>Hi</span>"));
        }

        [Fact]
        public void Render_Component_HtmlBlockEncoded()
        {
            // Arrange
            var component = CompileToComponent(@"<div>&lt;span&gt;Hi&lt/span&gt;</div>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Markup(frame, "<div>&lt;span&gt;Hi&lt/span&gt;</div>"));
        }

        // Integration test for HTML block rewriting
        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/6183")]
        public void Render_HtmlBlock_Integration()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
"));

            var component = CompileToComponent(@"
<html>
  <head><meta><meta></head>
  <body>
    <MyComponent>
      <div><span></span><span></span></div>
      <div>@(""hi"")</div>
      <div><span></span><span></span></div>
      <div></div>
      <div>@(""hi"")</div>
      <div></div>
  </MyComponent>
  </body>
</html>");

            // Act
            var frames = GetRenderTree(component);

            // Assert: component frames are correct
            Assert.Collection(
                frames,
                frame => AssertFrame.Element(frame, "html", 9, 0),
                frame => AssertFrame.MarkupWhitespace(frame, 1),
                frame => AssertFrame.Markup(frame, "<head><meta><meta></head>\n  ", 2),
                frame => AssertFrame.Element(frame, "body", 5, 3),
                frame => AssertFrame.MarkupWhitespace(frame, 4),
                frame => AssertFrame.Component(frame, "Test.MyComponent", 2, 5),
                frame => AssertFrame.Attribute(frame, "ChildContent", 6),
                frame => AssertFrame.MarkupWhitespace(frame, 16),
                frame => AssertFrame.MarkupWhitespace(frame, 17));

            // Assert: Captured ChildContent frames are correct
            var childFrames = GetFrames((RenderFragment)frames[6].AttributeValue);
            Assert.Collection(
                childFrames.AsEnumerable(),
                frame => AssertFrame.MarkupWhitespace(frame, 7),
                frame => AssertFrame.Markup(frame, "<div><span></span><span></span></div>\n      ", 8),
                frame => AssertFrame.Element(frame, "div", 2, 9),
                frame => AssertFrame.Text(frame, "hi", 10),
                frame => AssertFrame.MarkupWhitespace(frame, 11),
                frame => AssertFrame.Markup(frame, "<div><span></span><span></span></div>\n      <div></div>\n      ", 12),
                frame => AssertFrame.Element(frame, "div", 2, 13),
                frame => AssertFrame.Text(frame, "hi", 14),
                frame => AssertFrame.Markup(frame, "\n      <div></div>\n  ", 15));
        }

        [Fact]
        public void RazorTemplate_CanBeUsedFromComponent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Test
{
    public class Repeater : ComponentBase
    {
        [Parameter] public int Count { get; set; }
        [Parameter] public RenderFragment<string> Template { get; set; }
        [Parameter] public string Value { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            for (var i = 0; i < Count; i++)
            {
                builder.AddContent(i, Template, Value);
            }
        }
    }
}
"));

            var component = CompileToComponent(@"
@{ RenderFragment<string> template = (context) => @<div>@context.ToLower()</div>; }
<Repeater Count=3 Value=""Hello, World!"" Template=""template"" />
");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.Repeater", 4, 2),
                frame => AssertFrame.Attribute(frame, "Count", typeof(int), 3),
                frame => AssertFrame.Attribute(frame, "Value", typeof(string), 4),
                frame => AssertFrame.Attribute(frame, "Template", typeof(RenderFragment<string>), 5),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hello, world!", 1));
        }
    }
}
