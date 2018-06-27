// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class WhiteSpaceRewriterTest : CsHtmlMarkupParserTestBase
    {
        public WhiteSpaceRewriterTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void Rewrite_Moves_Whitespace_Preceeding_ExpressionBlock_To_Parent_Block()
        {
            // Arrange
            var parsed = ParseDocument(
                RazorLanguageVersion.Latest,
                "test    @foo test",
                Array.Empty<DirectiveDescriptor>());

            var rewriter = new WhiteSpaceRewriter();

            // Act
            var rewritten = rewriter.Rewrite(parsed.Root);

            // Assert
            BaselineTest(parsed);
        }
    }
}
