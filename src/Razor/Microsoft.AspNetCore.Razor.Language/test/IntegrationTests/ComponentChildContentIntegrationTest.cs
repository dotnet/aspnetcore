// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentChildContentIntegrationTest : RazorIntegrationTestBase
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

        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void ChildContent_AttributeAndBody_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@{ RenderFragment<string> template = @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template.WithValue(""HI"")"">
Some Content
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentSetByAttributeAndBody.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_AttributeAndExplicitChildContent_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@{ RenderFragment<string> template = @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template.WithValue(""HI"")"">
<ChildContent>
Some Content
</ChildContent>
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentSetByAttributeAndBody.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_UnrecogizedContent_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContent>
<ChildContent>
</ChildContent>
@somethingElse
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentMixedWithExplicitChildContent.Id, diagnostic.Id);
            Assert.Equal(
                "Unrecognized child content inside component 'RenderChildContent'. The component 'RenderChildContent' accepts " +
                "child content through the following top-level items: 'ChildContent'.",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ChildContent_ExplicitChildContent_UnrecogizedElement_ProducesDiagnostic(bool supportLocalizedComponentNames)
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContent>
<ChildContent>
</ChildContent>
<UnrecognizedChildContent></UnrecognizedChildContent>
</RenderChildContent>", supportLocalizedComponentNames: supportLocalizedComponentNames);

            // Assert
            Assert.Collection(
                generated.Diagnostics,
                d => Assert.Equal("RZ10012", d.Id),
                d => Assert.Equal("RZ9996", d.Id));
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_StartsWithCharThatIsOtherLetterCategory_WhenLocalizedComponentNamesIsAllowed()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@$"
<RenderChildContent>
<ChildContent>
</ChildContent>
<繁体字></繁体字>
</RenderChildContent>", supportLocalizedComponentNames: true);

            // Assert
            Assert.Collection(
                generated.Diagnostics,
                d => Assert.Equal("RZ10012", d.Id),
                d => Assert.Equal("RZ9996", d.Id));
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_StartsWithCharThatIsOtherLetterCategory_WhenLocalizedComponentNamesIsDisallowed()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@$"
<RenderChildContent>
<ChildContent>
</ChildContent>
<繁体字></繁体字>
</RenderChildContent>", supportLocalizedComponentNames: false);

            // Assert
            Assert.Collection(
                generated.Diagnostics,
                d => Assert.Equal("RZ9996", d.Id));
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_UnrecogizedAttribute_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContent>
<ChildContent attr>
</ChildContent>
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentHasInvalidAttribute.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_InvalidParameterName_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContentString>
<ChildContent Context=""@(""HI"")"">
</ChildContent>
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentHasInvalidParameter.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_RepeatedParameterName_GeneratesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
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
            Assert.Same(ComponentDiagnosticFactory.ChildContentRepeatedParameterName.Id, diagnostic.Id);
            Assert.Equal(
                "The child content element 'ChildContent' of component 'RenderChildContentString' uses the same parameter name ('context') as enclosing child content " +
                "element 'ChildContent' of component 'RenderChildContentString'. Specify the parameter name like: '<ChildContent Context=\"another_name\"> to resolve the ambiguity",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void ChildContent_ContextParameterNameOnComponent_Invalid_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContentString Context=""@Foo()"">
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentHasInvalidParameterOnComponent.Id, diagnostic.Id);
            Assert.Equal(
                "Invalid parameter name. The parameter name attribute 'Context' on component 'RenderChildContentString' can only include literal text.",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_ContainsDirectiveAttribute_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContentString>
<ChildContent Context=""items"" @key=""Hello"">
</ChildContent>
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentHasInvalidAttribute.Id, diagnostic.Id);
            Assert.Equal(
                "Unrecognized attribute '@key' on child content element 'ChildContent'.",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }
    }
}
