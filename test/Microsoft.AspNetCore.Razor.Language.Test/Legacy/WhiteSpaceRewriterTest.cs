// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class WhiteSpaceRewriterTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void Moves_Whitespace_Preceeding_ExpressionBlock_To_Parent_Block()
        {
            // Arrange
            var content = @"
<div>
    @result
</div>
<div>
    @(result)
</div>";
            var parsed = ParseDocument(
                RazorLanguageVersion.Latest,
                content,
                Array.Empty<DirectiveDescriptor>());

            var rewriter = new WhitespaceRewriter();

            // Act
            var rewritten = rewriter.Visit(parsed.Root);

            // Assert
            var rewrittenTree = RazorSyntaxTree.Create(rewritten, parsed.Source, parsed.Diagnostics, parsed.Options);
            BaselineTest(rewrittenTree);
        }
    }
}
