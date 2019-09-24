// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class ChildContentRazorIntegrationTest : RazorIntegrationTestBase
    {
        private readonly CSharpSyntaxTree RenderChildContentComponent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
namespace Test
{
    public class RenderChildContent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent);
        }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
");

        private readonly CSharpSyntaxTree RenderChildContentStringComponent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
namespace Test
{
    public class RenderChildContentString : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent, Value);
        }

        [Parameter]
        public RenderFragment<string> ChildContent { get; set; }

        [Parameter]
        public string Value { get; set; }
    }
}
");

        private readonly CSharpSyntaxTree RenderMultipleChildContent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
namespace Test
{
    public class RenderMultipleChildContent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Header, Name);
            builder.AddContent(1, ChildContent, Value);
            builder.AddContent(2, Footer);
        }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public RenderFragment<string> Header { get; set; }

        [Parameter]
        public RenderFragment<string> ChildContent { get; set; }

        [Parameter]
        public RenderFragment Footer { get; set; }

        [Parameter]
        public string Value { get; set; }
    }
}
");

        public ChildContentRazorIntegrationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void Render_BodyChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
<RenderChildContent>
  <div></div>
</RenderChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 0),
                frame => AssertFrame.Attribute(frame, "ChildContent", 1),
                frame => AssertFrame.Markup(frame, "\n  <div></div>\n", 2));
        }

        [Fact]
        public void Render_BodyChildContent_Generic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            var component = CompileToComponent(@"
<RenderChildContentString Value=""HI"">
  <div>@context.ToLowerInvariant()</div>
</RenderChildContentString>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContentString", 3, 0),
                frame => AssertFrame.Attribute(frame, "Value", "HI", 1),
                frame => AssertFrame.Attribute(frame, "ChildContent", 2),
                frame => AssertFrame.MarkupWhitespace(frame, 3),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "hi", 5),
                frame => AssertFrame.MarkupWhitespace(frame, 6));
        }

        [Fact]
        public void Render_ExplicitChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
<RenderChildContent>
  <ChildContent>
    <div></div>
  </ChildContent>
</RenderChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 0),
                frame => AssertFrame.Attribute(frame, "ChildContent", 1),
                frame => AssertFrame.Markup(frame, "\n    <div></div>\n  ", 2));
        }

        [Fact]
        public void Render_BodyChildContent_Recursive()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"

<RenderChildContent>
  <RenderChildContent>
    <div></div>
  </RenderChildContent>
</RenderChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 0),
                frame => AssertFrame.Attribute(frame, "ChildContent", 1),
                frame => AssertFrame.MarkupWhitespace(frame, 2),
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 3),
                frame => AssertFrame.Attribute(frame, "ChildContent", 4),
                frame => AssertFrame.MarkupWhitespace(frame, 6),
                frame => AssertFrame.Markup(frame, "\n    <div></div>\n  ", 5));
        }

        [Fact]
        public void Render_AttributeChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@{ RenderFragment<string> template = (context) => @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template(""HI"")"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 2),
                frame => AssertFrame.Attribute(frame, "ChildContent", 3),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_AttributeChildContent_RenderFragmentOfString()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            var component = CompileToComponent(@"
@{ RenderFragment<string> template = (context) => @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContentString ChildContent=""@template"" Value=""HI"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContentString", 3, 2),
                frame => AssertFrame.Attribute(frame, "ChildContent", 3),
                frame => AssertFrame.Attribute(frame, "Value", "HI", 4),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_AttributeChildContent_NoArgTemplate()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@{ RenderFragment template = @<div>@(""HI"".ToLowerInvariant())</div>; }
<RenderChildContent ChildContent=""@template"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 2),
                frame => AssertFrame.Attribute(frame, "ChildContent", 3),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_AttributeChildContent_IgnoresEmptyBody()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@{ RenderFragment<string> template = (context) => @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template(""HI"")""></RenderChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 2),
                frame => AssertFrame.Attribute(frame, "ChildContent", 3),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_AttributeChildContent_IgnoresWhitespaceBody()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@{ RenderFragment<string> template = (context) => @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template(""HI"")"">

</RenderChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 2),
                frame => AssertFrame.Attribute(frame, "ChildContent", 3),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_MultipleChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderMultipleChildContent);

            var component = CompileToComponent(@"
@{ RenderFragment<string> header = context => @<div>@context.ToLowerInvariant()</div>; }
<RenderMultipleChildContent Name=""billg"" Header=@header Value=""HI"">
  <ChildContent>Some @context.ToLowerInvariant() Content</ChildContent>
  <Footer>Bye!</Footer>
</RenderMultipleChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderMultipleChildContent", 6, 2),
                frame => AssertFrame.Attribute(frame, "Name", "billg", 3),
                frame => AssertFrame.Attribute(frame, "Header", typeof(RenderFragment<string>), 4),
                frame => AssertFrame.Attribute(frame, "Value", "HI", 5),
                frame => AssertFrame.Attribute(frame, "ChildContent", typeof(RenderFragment<string>), 6),
                frame => AssertFrame.Attribute(frame, "Footer", typeof(RenderFragment), 10),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "billg", 1),
                frame => AssertFrame.Text(frame, "Some ", 7),
                frame => AssertFrame.Text(frame, "hi", 8),
                frame => AssertFrame.Text(frame, " Content", 9),
                frame => AssertFrame.Text(frame, "Bye!", 11));
        }

        [Fact]
        public void Render_MultipleChildContent_ContextParameterOnComponent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderMultipleChildContent);

            var component = CompileToComponent(@"
<RenderMultipleChildContent Name=""billg"" Value=""HI"" Context=""item"">
  <Header><div>@item.ToLowerInvariant()</div></Header>
  <ChildContent Context=""Context"">Some @Context.ToLowerInvariant() Content</ChildContent>
  <Footer>Bye!</Footer>
</RenderMultipleChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderMultipleChildContent", 6, 0),
                frame => AssertFrame.Attribute(frame, "Name", "billg", 1),
                frame => AssertFrame.Attribute(frame, "Value", "HI", 2),
                frame => AssertFrame.Attribute(frame, "Header", typeof(RenderFragment<string>), 3),
                frame => AssertFrame.Attribute(frame, "ChildContent", typeof(RenderFragment<string>), 6),
                frame => AssertFrame.Attribute(frame, "Footer", typeof(RenderFragment), 10),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "billg", 5),
                frame => AssertFrame.Text(frame, "Some ", 7),
                frame => AssertFrame.Text(frame, "hi", 8),
                frame => AssertFrame.Text(frame, " Content", 9),
                frame => AssertFrame.Text(frame, "Bye!", 11));
        }

        // Verifies that our check for reuse of parameter names isn't too aggressive.
        [Fact]
        public void Render_MultipleChildContent_ContextParameterOnComponent_SetsSameName()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderMultipleChildContent);

            var component = CompileToComponent(@"

<RenderMultipleChildContent Name=""billg"" Value=""HI"" Context=""item"">
  <Header><div>@item.ToLowerInvariant()</div></Header>
  <ChildContent Context=""item"">Some @item.ToLowerInvariant() Content</ChildContent>
  <Footer>Bye!</Footer>
</RenderMultipleChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderMultipleChildContent", 6, 0),
                frame => AssertFrame.Attribute(frame, "Name", "billg", 1),
                frame => AssertFrame.Attribute(frame, "Value", "HI", 2),
                frame => AssertFrame.Attribute(frame, "Header", typeof(RenderFragment<string>), 3),
                frame => AssertFrame.Attribute(frame, "ChildContent", typeof(RenderFragment<string>), 6),
                frame => AssertFrame.Attribute(frame, "Footer", typeof(RenderFragment), 10),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "billg", 5),
                frame => AssertFrame.Text(frame, "Some ", 7),
                frame => AssertFrame.Text(frame, "hi", 8),
                frame => AssertFrame.Text(frame, " Content", 9),
                frame => AssertFrame.Text(frame, "Bye!", 11));
        }
    }
}
