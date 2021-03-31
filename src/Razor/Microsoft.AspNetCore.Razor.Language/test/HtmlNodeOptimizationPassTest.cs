// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class HtmlNodeOptimizationPassTest
    {
        [Fact]
        public void Execute_RewritesWhitespace()
        {
            // Assert
            var content = Environment.NewLine + "    @true";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            var pass = new HtmlNodeOptimizationPass();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            // Act
            var outputTree = pass.Execute(codeDocument, originalTree);

            // Assert
            var document = Assert.IsType<RazorDocumentSyntax>(outputTree.Root);
            var block = Assert.IsType<MarkupBlockSyntax>(document.Document);
            Assert.Equal(4, block.Children.Count);
            var whitespace = Assert.IsType<MarkupTextLiteralSyntax>(block.Children[1]);
            Assert.True(whitespace.GetContent().All(char.IsWhiteSpace));
        }
    }
}
