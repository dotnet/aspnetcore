// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test
{
    public class RazorSyntaxTreeTest
    {
        [Fact]
        public void Parse_CanParseEmptyDocument()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create(string.Empty);

            // Act
            var syntaxTree = RazorSyntaxTree.Parse(source);

            // Assert
            Assert.NotNull(syntaxTree);
            Assert.Empty(syntaxTree.Diagnostics);
        }

        [Fact]
        public void Parse_Persists_FilePath()
        {
            // Arrange
            var filePath = "test.cshtml";
            var source = TestRazorSourceDocument.Create("@if (true) { @if(false) { <div>@something.</div> } }", filePath: filePath);

            // Act
            var syntaxTree = RazorSyntaxTree.Parse(source);

            // Assert
            Assert.Empty(syntaxTree.Diagnostics);
            Assert.NotNull(syntaxTree);

            var spans = new List<SyntaxTreeNode>();
            GetChildren(syntaxTree.Root);
            Assert.All(spans, node => Assert.Equal(filePath, node.Start.FilePath));

            void GetChildren(SyntaxTreeNode node)
            {
                if (node is Block block)
                {
                    foreach (var child in block.Children)
                    {
                        GetChildren(child);
                    }
                }
                else
                {
                    spans.Add(node);
                }
            }
        }

        [Fact]
        public void Parse_UseDirectiveTokenizer_ParsesUntilFirstDirective()
        {
            // Arrange
            var source = TestRazorSourceDocument.Create("\r\n  \r\n    @*SomeComment*@ \r\n  @tagHelperPrefix \"SomePrefix\"\r\n<html>\r\n@if (true) {\r\n @if(false) { <div>@something.</div> } \r\n}");
            var options = RazorParserOptions.Create(builder => builder.ParseLeadingDirectives = true);

            // Act
            var syntaxTree = RazorSyntaxTree.Parse(source, options);

            // Assert
            Assert.NotNull(syntaxTree);
            Assert.Equal(6, syntaxTree.Root.Children.Count);
            var block = Assert.IsType<Block>(syntaxTree.Root.Children[4]);
            Assert.Equal(BlockKindInternal.Directive, block.Type);
            Assert.Empty(syntaxTree.Diagnostics);
        }
    }
}
