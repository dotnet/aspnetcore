// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Components.Razor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    public class ChildContentRazorIntegrationTest : RazorIntegrationTestBase
    {
        private readonly CSharpSyntaxTree RenderChildContentComponent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
namespace Test
{
    public class RenderChildContent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent);
        }

        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
");

        private readonly CSharpSyntaxTree RenderChildContentStringComponent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
namespace Test
{
    public class RenderChildContentString : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent, Value);
        }

        [Parameter]
        RenderFragment<string> ChildContent { get; set; }

        [Parameter]
        string Value { get; set; }
    }
}
");

        private readonly CSharpSyntaxTree RenderMultipleChildContent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
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
        string Name { get; set; }

        [Parameter]
        RenderFragment<string> Header { get; set; }

        [Parameter]
        RenderFragment<string> ChildContent { get; set; }

        [Parameter]
        RenderFragment Footer { get; set; }

        [Parameter]
        string Value { get; set; }
    }
}
");

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void Render_BodyChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
<RenderChildContent>
  <div></div>
</RenderChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 0),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 1),
                frame => AssertFrame.Markup(frame, "\n  <div></div>\n", 2));
        }

        [Fact]
        public void Render_BodyChildContent_Generic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
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
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 2),
                frame => AssertFrame.Whitespace(frame, 3),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "hi", 5),
                frame => AssertFrame.Whitespace(frame, 6));
        }

        [Fact]
        public void Render_ExplicitChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
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
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 1),
                frame => AssertFrame.Markup(frame, "\n    <div></div>\n  ", 2));
        }

        [Fact]
        public void Render_BodyChildContent_Recursive()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
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
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 1),
                frame => AssertFrame.Whitespace(frame, 2),
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 3),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 4),
                frame => AssertFrame.Whitespace(frame, 6),
                frame => AssertFrame.Markup(frame, "\n    <div></div>\n  ", 5));
        }

        [Fact]
        public void Render_AttributeChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
@{ RenderFragment<string> template = (context) => @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template(""HI"")"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 2),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 3),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_AttributeChildContent_RenderFragmentOfString()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
@{ RenderFragment<string> template = (context) => @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContentString ChildContent=""@template"" Value=""HI"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContentString", 3, 2),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 3),
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
@addTagHelper *, TestAssembly
@{ RenderFragment template = @<div>@(""HI"".ToLowerInvariant())</div>; }
<RenderChildContent ChildContent=""@template"" />");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 2),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 3),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_AttributeChildContent_IgnoresEmptyBody()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
@{ RenderFragment<string> template = (context) => @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template(""HI"")""></RenderChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 2),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 3),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_AttributeChildContent_IgnoresWhitespaceBody()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
@{ RenderFragment<string> template = (context) => @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template(""HI"")"">
       
</RenderChildContent>");

            // Act
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(
                frames,
                frame => AssertFrame.Component(frame, "Test.RenderChildContent", 2, 2),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 3),
                frame => AssertFrame.Element(frame, "div", 2, 0),
                frame => AssertFrame.Text(frame, "hi", 1));
        }

        [Fact]
        public void Render_MultipleChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderMultipleChildContent);

            var component = CompileToComponent(@"
@addTagHelper *, TestAssembly
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
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, typeof(RenderFragment<string>), 6),
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
@addTagHelper *, TestAssembly
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
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, typeof(RenderFragment<string>), 6),
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
@addTagHelper *, TestAssembly
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
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, typeof(RenderFragment<string>), 6),
                frame => AssertFrame.Attribute(frame, "Footer", typeof(RenderFragment), 10),
                frame => AssertFrame.Element(frame, "div", 2, 4),
                frame => AssertFrame.Text(frame, "billg", 5),
                frame => AssertFrame.Text(frame, "Some ", 7),
                frame => AssertFrame.Text(frame, "hi", 8),
                frame => AssertFrame.Text(frame, " Content", 9),
                frame => AssertFrame.Text(frame, "Bye!", 11));
        }

        [Fact]
        public void Render_ChildContent_AttributeAndBody_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@{ RenderFragment<string> template = @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template.WithValue(""HI"")"">
Some Content
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.ChildContentSetByAttributeAndBody.Id, diagnostic.Id);
        }

        [Fact]
        public void Render_ChildContent_AttributeAndExplicitChildContent_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@{ RenderFragment<string> template = @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template.WithValue(""HI"")"">
<ChildContent>
Some Content
</ChildContent>
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.ChildContentSetByAttributeAndBody.Id, diagnostic.Id);
        }

        [Fact]
        public void Render_ChildContent_ExplicitChildContent_UnrecogizedContent_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<RenderChildContent>
<ChildContent>
</ChildContent>
@somethingElse
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.ChildContentMixedWithExplicitChildContent.Id, diagnostic.Id);
            Assert.Equal(
                "Unrecognized child content inside component 'RenderChildContent'. The component 'RenderChildContent' accepts " +
                "child content through the following top-level items: 'ChildContent'.",
                diagnostic.GetMessage());
        }

        [Fact]
        public void Render_ChildContent_ExplicitChildContent_UnrecogizedElement_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<RenderChildContent>
<ChildContent>
</ChildContent>
<UnrecognizedChildContent></UnrecognizedChildContent>
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.ChildContentMixedWithExplicitChildContent.Id, diagnostic.Id);
        }

        [Fact]
        public void Render_ChildContent_ExplicitChildContent_UnrecogizedAttribute_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<RenderChildContent>
<ChildContent attr>
</ChildContent>
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.ChildContentHasInvalidAttribute.Id, diagnostic.Id);
        }

        [Fact]
        public void Render_ChildContent_ExplicitChildContent_InvalidParameterName_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<RenderChildContentString>
<ChildContent Context=""@(""HI"")"">
</ChildContent>
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.ChildContentHasInvalidParameter.Id, diagnostic.Id);
        }

        [Fact]
        public void Render_ChildContent_ExplicitChildContent_RepeatedParameterName_GeneratesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<RenderChildContentString>
<ChildContent>
<RenderChildContentString>
<ChildContent Context=""context"">
</ChildContent>
</RenderChildContentString>
</ChildContent>
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.ChildContentRepeatedParameterName.Id, diagnostic.Id);
            Assert.Equal(
                "The child content element 'ChildContent' of component 'RenderChildContentString' uses the same parameter name ('context') as enclosing child content " +
                "element 'ChildContent' of component 'RenderChildContentString'. Specify the parameter name like: '<ChildContent Context=\"another_name\"> to resolve the ambiguity",
                diagnostic.GetMessage());
        }

        [Fact]
        public void Render_ChildContent_ContextParameterNameOnComponent_Invalid_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<RenderChildContentString Context=""@Foo()"">
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(BlazorDiagnosticFactory.ChildContentHasInvalidParameterOnComponent.Id, diagnostic.Id);
            Assert.Equal(
                "Invalid parameter name. The parameter name attribute 'Context' on component 'RenderChildContentString' can only include literal text.",
                diagnostic.GetMessage());
        }
    }
}
