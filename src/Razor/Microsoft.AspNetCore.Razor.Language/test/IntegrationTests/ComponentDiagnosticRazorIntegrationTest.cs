// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentDiagnosticRazorIntegrationTest : RazorIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component;

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
                    Assert.Equal("Unexpected closing tag 'mytag' with no matching start tag.", item.GetMessage());
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
            Assert.NotNull(diagnostic.GetMessage());

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
                    Assert.Equal("Script tags should not be placed inside components because they cannot be updated dynamically. To fix this, move the script tag to the 'index.html' file or another static location. For more information see https://go.microsoft.com/fwlink/?linkid=872131", item.GetMessage());
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
                "Use '@using <namespace>' directive instead.", item.GetMessage());
                    Assert.Equal(0, item.Span.LineIndex);
                    Assert.Equal(0, item.Span.CharacterIndex);
                },
                item =>
                {
                    Assert.Equal("RZ9978", item.Id);
                    Assert.Equal("The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. " +
                "Use '@using <namespace>' directive instead.", item.GetMessage());
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
                diagnostic.GetMessage());
        }
    }
}
