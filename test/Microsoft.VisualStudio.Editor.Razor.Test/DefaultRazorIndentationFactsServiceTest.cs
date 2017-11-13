// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultRazorIndentationFactsServiceTest
    {
        [Fact]
        public void GetDesiredIndentation_ReturnsNull_IfOwningSpanIsCode()
        {
            // Arrange
            var source = new StringTextSnapshot($@"
@{{
");
            var syntaxTree = GetSyntaxTree(source);
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentation = service.GetDesiredIndentation(
                syntaxTree,
                source,
                source.GetLineFromLineNumber(2),
                indentSize: 4,
                tabSize: 1);

            // Assert
            Assert.Null(indentation);
        }

        [Fact]
        public void GetDesiredIndentation_ReturnsNull_IfOwningSpanIsNone()
        {
            // Arrange
            var customDirective = DirectiveDescriptor.CreateSingleLineDirective("custom");
            var source = new StringTextSnapshot($@"
@custom
");
            var syntaxTree = GetSyntaxTree(source, new[] { customDirective });
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentation = service.GetDesiredIndentation(
                syntaxTree,
                source,
                source.GetLineFromLineNumber(2),
                indentSize: 4,
                tabSize: 1);

            // Assert
            Assert.Null(indentation);
        }

        [Fact]
        public void GetDesiredIndentation_ReturnsCorrectIndentation_ForMarkupWithinCodeBlock()
        {
            // Arrange
            var source = new StringTextSnapshot($@"@{{
    <div>
");
            var syntaxTree = GetSyntaxTree(source);
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentation = service.GetDesiredIndentation(
                syntaxTree,
                source,
                source.GetLineFromLineNumber(2),
                indentSize: 4,
                tabSize: 4);

            // Assert
            Assert.Equal(4, indentation);
        }

        [Fact]
        public void GetDesiredIndentation_ReturnsCorrectIndentation_ForMarkupWithinDirectiveBlock()
        {
            // Arrange
            var customDirective = DirectiveDescriptor.CreateRazorBlockDirective("custom");
            var source = new StringTextSnapshot($@"@custom
{{
    <div>
}}");
            var syntaxTree = GetSyntaxTree(source, new[] { customDirective });
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentation = service.GetDesiredIndentation(
                syntaxTree,
                source,
                source.GetLineFromLineNumber(3),
                indentSize: 4,
                tabSize: 4);

            // Assert
            Assert.Equal(4, indentation);
        }

        [Fact]
        public void GetDesiredIndentation_ReturnsCorrectIndentation_ForNestedMarkupWithinCodeBlock()
        {
            // Arrange
            var source = new StringTextSnapshot($@"
<div>
    @{{
        <span>
    }}
</div>
");
            var syntaxTree = GetSyntaxTree(source);
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentation = service.GetDesiredIndentation(
                syntaxTree,
                source,
                source.GetLineFromLineNumber(4),
                indentSize: 4,
                tabSize: 4);

            // Assert
            Assert.Equal(8, indentation);
        }

        [Fact]
        public void GetDesiredIndentation_ReturnsCorrectIndentation_ForMarkupWithinCodeBlockInADirectiveBlock()
        {
            // Arrange
            var customDirective = DirectiveDescriptor.CreateRazorBlockDirective("custom");
            var source = new StringTextSnapshot($@"@custom
{{
    @{{
        <div>
    }}
}}");
            var syntaxTree = GetSyntaxTree(source, new[] { customDirective });
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentation = service.GetDesiredIndentation(
                syntaxTree,
                source,
                source.GetLineFromLineNumber(4),
                indentSize: 4,
                tabSize: 4);

            // Assert
            Assert.Equal(8, indentation);
        }

        private static RazorSyntaxTree GetSyntaxTree(StringTextSnapshot source, IEnumerable<DirectiveDescriptor> directives = null)
        {
            directives = directives ?? Enumerable.Empty<DirectiveDescriptor>();
            var engine = RazorEngine.CreateDesignTime(builder =>
            {
                foreach (var directive in directives)
                {
                    builder.AddDirective(directive);
                }
            });

            var sourceDocument = RazorSourceDocument.Create(source.GetText(), "test.cshtml");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            engine.Process(codeDocument);

            return codeDocument.GetSyntaxTree();
        }
    }
}
