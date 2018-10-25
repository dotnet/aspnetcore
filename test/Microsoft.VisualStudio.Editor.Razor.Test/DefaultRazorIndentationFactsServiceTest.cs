// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultRazorIndentationFactsServiceTest
    {
        [Fact]
        public void GetPreviousLineEndIndex_ReturnsPreviousLine()
        {
            // Arrange
            var textSnapshot = new StringTextSnapshot(@"@{
    <p>Hello World</p>
}");
            var line = textSnapshot.GetLineFromLineNumber(2);

            // Act
            var previousLineEndIndex = DefaultRazorIndentationFactsService.GetPreviousLineEndIndex(textSnapshot, line);

            // Assert
            Assert.Equal(24 + Environment.NewLine.Length, previousLineEndIndex);
        }

        [Fact]
        public void IsCSharpOpenCurlyBrace_SpanWithLeftBrace_ReturnTrue()
        {
            // Arrange
            var childBuilder = new SpanBuilder(SourceLocation.Zero);
            childBuilder.Accept(SyntaxFactory.Token(SyntaxKind.LeftBrace, "{"));
            var child = childBuilder.Build();

            // Act
            var result = DefaultRazorIndentationFactsService.IsCSharpOpenCurlyBrace(child);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("if", SyntaxKind.Keyword)]
        [InlineData("}", SyntaxKind.RightBrace)]
        [InlineData("++", SyntaxKind.Increment)]
        [InlineData("text", SyntaxKind.Identifier)]
        public void IsCSharpOpenCurlyBrace_SpanWithUnsupportedSymbolType_ReturnFalse(string content, object symbolTypeObject)
        {
            // Arrange
            var symbolType = (SyntaxKind)symbolTypeObject;
            var childBuilder = new SpanBuilder(SourceLocation.Zero);
            childBuilder.Accept(SyntaxFactory.Token(symbolType, content));
            var child = childBuilder.Build();

            // Act
            var result = DefaultRazorIndentationFactsService.IsCSharpOpenCurlyBrace(child);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCSharpOpenCurlyBrace_MultipleSymbols_ReturnFalse()
        {
            // Arrange
            var childBuilder = new SpanBuilder(SourceLocation.Zero);
            childBuilder.Accept(SyntaxFactory.Token(SyntaxKind.Identifier, "hello"));
            childBuilder.Accept(SyntaxFactory.Token(SyntaxKind.Comma, ","));
            var child = childBuilder.Build();

            // Act
            var result = DefaultRazorIndentationFactsService.IsCSharpOpenCurlyBrace(child);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCSharpOpenCurlyBrace_SpanWithHtmlSymbol_ReturnFalse()
        {
            // Arrange
            var childBuilder = new SpanBuilder(SourceLocation.Zero);
            childBuilder.Accept(SyntaxFactory.Token(SyntaxKind.Text, "hello"));
            var child = childBuilder.Build();

            // Act
            var result = DefaultRazorIndentationFactsService.IsCSharpOpenCurlyBrace(child);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCSharpOpenCurlyBrace_Blocks_ReturnFalse()
        {
            // Arrange
            var child = new BlockBuilder()
            {
                Type = BlockKindInternal.Markup,
            }.Build();

            // Act
            var result = DefaultRazorIndentationFactsService.IsCSharpOpenCurlyBrace(child);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetIndentLevelOfLine_AddsTabsOnlyAtBeginningOfLine()
        {
            // Arrange
            var text = "\t\tHello\tWorld.\t";
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentLevel = service.GetIndentLevelOfLine(text, 4);

            // Assert
            Assert.Equal(8, indentLevel);
        }

        [Fact]
        public void GetIndentLevelOfLine_AddsSpacesOnlyAtBeginningOfLine()
        {
            // Arrange
            var text = "   Hello World. ";
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentLevel = service.GetIndentLevelOfLine(text, 4);

            // Assert
            Assert.Equal(3, indentLevel);
        }

        [Fact]
        public void GetIndentLevelOfLine_AddsTabsAndSpacesOnlyAtBeginningOfLine()
        {
            // Arrange
            var text = "  \t \tHello\t World.\t ";
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentLevel = service.GetIndentLevelOfLine(text, 4);

            // Assert
            Assert.Equal(11, indentLevel);
        }

        [Fact]
        public void GetIndentLevelOfLine_NoIndent()
        {
            // Arrange
            var text = "Hello World.";
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentLevel = service.GetIndentLevelOfLine(text, 4);

            // Assert
            Assert.Equal(0, indentLevel);
        }

        // This test verifies that we still operate on SyntaxTree's that have gaps in them. The gaps are temporary
        // until our work with the parser has been completed.
        [Fact]
        public void GetDesiredIndentation_ReturnsNull_IfOwningSpanDoesNotExist()
        {
            // Arrange
            var source = new StringTextSnapshot($@"
<div>
    <div>
    </div>
</div>
");
            var syntaxTree = GetSyntaxTree(new StringTextSnapshot("something else"));
            var service = new DefaultRazorIndentationFactsService();

            // Act
            var indentation = service.GetDesiredIndentation(
                syntaxTree,
                source,
                source.GetLineFromLineNumber(3),
                indentSize: 4,
                tabSize: 1);

            // Assert
            Assert.Null(indentation);
        }

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
            var engine = RazorProjectEngine.Create(builder =>
            {
                foreach (var directive in directives)
                {
                    builder.AddDirective(directive);
                }
            });

            var sourceProjectItem = new TestRazorProjectItem("test.cshtml")
            {
                Content = source.GetText()
            };

            var codeDocument = engine.ProcessDesignTime(sourceProjectItem);

            return codeDocument.GetSyntaxTree();
        }
    }
}
