// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class HtmlNodeOptimizationPassTest
    {
        [Fact]
        public void Execute_CollapsesConditionalAttributes()
        {
            // Assert
            var content = "<input value='hello world' />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            var pass = new HtmlNodeOptimizationPass();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            // Act
            var outputTree = pass.Execute(codeDocument, originalTree);

            // Assert
            var tag = Assert.Single(outputTree.Root.Children);
            var tagBlock = Assert.IsType<Block>(tag);
            Assert.Equal(BlockKindInternal.Tag, tagBlock.Type);
            Assert.Equal(3, tagBlock.Children.Count);
            Assert.IsType<Span>(tagBlock.Children[1]);
        }

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
            Assert.Equal(4, outputTree.Root.Children.Count);
            var whitespace = Assert.IsType<Span>(outputTree.Root.Children[1]);
            Assert.True(whitespace.Content.All(char.IsWhiteSpace));
        }
    }
}
