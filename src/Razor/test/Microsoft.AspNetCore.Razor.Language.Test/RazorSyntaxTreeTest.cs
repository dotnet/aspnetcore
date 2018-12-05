// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;
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
        public void Parse_NodesReturnCorrectFilePath()
        {
            // Arrange
            var filePath = "test.cshtml";
            var source = TestRazorSourceDocument.Create("@if (true) { @if(false) { <div>@something.</div> } }", filePath: filePath);

            // Act
            var syntaxTree = RazorSyntaxTree.Parse(source);

            // Assert
            Assert.Empty(syntaxTree.Diagnostics);
            Assert.NotNull(syntaxTree);

            var children = syntaxTree.Root.DescendantNodes();
            Assert.All(children, node => Assert.Equal(filePath, node.GetSourceLocation(source).FilePath));
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
            var root = syntaxTree.Root;
            Assert.NotNull(syntaxTree);
            Assert.Equal(61, root.EndPosition);
            Assert.Single(root.DescendantNodes().Where(n => n is RazorDirectiveBodySyntax body && body.Keyword.GetContent() == "tagHelperPrefix"));
            Assert.Empty(root.DescendantNodes().Where(n => n is MarkupTagBlockSyntax));
            Assert.Empty(syntaxTree.Diagnostics);
        }
    }
}
