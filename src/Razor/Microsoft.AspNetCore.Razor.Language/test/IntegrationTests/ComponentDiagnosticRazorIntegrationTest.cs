// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentDiagnosticRazorIntegrationTest : RazorIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void RejectsEndTagWithNoStartTag()
        {
            // Arrange/Act
            var result = CompileToCSharp(
                "Line1\nLine2\nLine3</mytag>");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal("RZ9981", item.Id);
                    Assert.Equal("Unexpected closing tag 'mytag' with no matching start tag.", item.GetMessage(CultureInfo.CurrentCulture));
                });
        }

        // This used to be a sugar syntax for lambdas, but we don't support that anymore
        [Fact]
        public void OldCodeBlockAttributeSyntax_ReportsError()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<elem attr=@{ DidInvokeCode = true; } />
@functions {
    public bool DidInvokeCode { get; set; } = false;
}");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ9979", diagnostic.Id);
            Assert.NotNull(diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void RejectsScriptTag()
        {
            // Arrange/Act
            var result = CompileToCSharp(@"Hello
<div>
    <script src='anything'>
        something
    </script>
</div>
Goodbye");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal("RZ9992", item.Id);
                    Assert.Equal("Script tags should not be placed inside components because they cannot be updated dynamically. To fix this, move the script tag to the 'index.html' file or another static location. For more information, see https://aka.ms/AAe3qu3", item.GetMessage(CultureInfo.CurrentCulture));
                    Assert.Equal(2, item.Span.LineIndex);
                    Assert.Equal(4, item.Span.CharacterIndex);
                });
        }

        [Fact]
        public void RejectsTagHelperDirectives()
        {
            // Arrange/Act
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

            var result = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@tagHelperPrefix th

<MyComponent />
");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal("RZ9978", item.Id);
                    Assert.Equal("The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. " +
                "Use '@using <namespace>' directive instead.", item.GetMessage(CultureInfo.CurrentCulture));
                    Assert.Equal(0, item.Span.LineIndex);
                    Assert.Equal(0, item.Span.CharacterIndex);
                },
                item =>
                {
                    Assert.Equal("RZ9978", item.Id);
                    Assert.Equal("The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. " +
                "Use '@using <namespace>' directive instead.", item.GetMessage(CultureInfo.CurrentCulture));
                    Assert.Equal(1, item.Span.LineIndex);
                    Assert.Equal(0, item.Span.CharacterIndex);
                });
        }

        [Fact]
        public void DirectiveAttribute_ComplexContent_ReportsError()
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<input type=""text"" @key=""Foo @Text"" />
@functions {
    public string Text { get; set; } = ""text"";
}");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ9986", diagnostic.Id);
            Assert.Equal(
                "Component attributes do not support complex content (mixed C# and markup). Attribute: '@key', text: 'Foo @Text'",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void Component_StartsWithLowerCase_ReportsError()
        {
            // Arrange & Act
            var generated = CompileToCSharp("lowerCase.razor", @"
<input type=""text"" @bind=""Text"" />
@functions {
    public string Text { get; set; } = ""text"";
}", throwOnFailure: false);

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ10011", diagnostic.Id);
            Assert.Equal(
                "Component 'lowerCase' starts with a lowercase character. Component names cannot start with a lowercase character.",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Component_NotFound_ReportsWarning(bool supportLocalizedComponentNames)
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<PossibleComponent></PossibleComponent>

@functions {
    public string Text { get; set; } = ""text"";
}", supportLocalizedComponentNames: supportLocalizedComponentNames);

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ10012", diagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Equal(
                "Found markup element with unexpected name 'PossibleComponent'. If this is intended to be a component, add a @using directive for its namespace.",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void Component_NotFound_StartsWithOtherLetter_WhenLocalizedComponentNamesIsAllowed_ReportsWarning()
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<繁体字></繁体字>

@functions {
    public string Text { get; set; } = ""text"";
}", supportLocalizedComponentNames: true);

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ10012", diagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Equal(
                "Found markup element with unexpected name '繁体字'. If this is intended to be a component, add a @using directive for its namespace.",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void Component_NotFound_StartsWithOtherLetter_WhenLocalizedComponentNamesIsDisallowed()
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<繁体字></繁体字>

@functions {
    public string Text { get; set; } = ""text"";
}", supportLocalizedComponentNames: false);

            // Assert
            Assert.Empty(generated.Diagnostics);
        }

        [Fact]
        public void Element_DoesNotStartWithLowerCase_OverrideWithBang_NoWarning()
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<!PossibleComponent></!PossibleComponent>");

            // Assert
            Assert.Empty(generated.Diagnostics);
        }

        [Fact]
        public void Component_StartAndEndTagCaseMismatch_ReportsError()
        {
            // Arrange & Act
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));
            var generated = CompileToCSharp(@"
<MyComponent></mycomponent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ10013", diagnostic.Id);
            Assert.Equal(
                "The start tag name 'MyComponent' does not match the end tag name 'mycomponent'. Components must have matching start and end tag names (case-sensitive).",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }
    }
}
