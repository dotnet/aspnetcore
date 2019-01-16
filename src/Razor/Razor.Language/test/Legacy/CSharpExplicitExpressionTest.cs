// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpExplicitExpressionTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ShouldOutputZeroLengthCodeSpanIfExplicitExpressionIsEmpty()
        {
            ParseBlockTest("@()");
        }

        [Fact]
        public void ShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpr()
        {
            // ParseBlockShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpression
            ParseBlockTest("@(");
        }

        [Fact]
        public void ShouldAcceptEscapedQuoteInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"\")");
        }

        [Fact]
        public void ShouldAcceptEscapedQuoteInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\")");
        }

        [Fact]
        public void ShouldAcceptMultipleRepeatedEscapedQuoteInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\"\"\")");
        }

        [Fact]
        public void ShouldAcceptMultiLineVerbatimStrings()
        {
            ParseBlockTest(@"@(@""" + Environment.NewLine
                         + @"Foo" + Environment.NewLine
                         + @"Bar" + Environment.NewLine
                         + @"Baz" + Environment.NewLine
                         + @""")");
        }

        [Fact]
        public void ShouldAcceptMultipleEscapedQuotesInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"hello, world\\\"\")");
        }

        [Fact]
        public void ShouldAcceptMultipleEscapedQuotesInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"hello, world\"\"\")");
        }

        [Fact]
        public void ShouldAcceptConsecutiveEscapedQuotesInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"\\\"\")");
        }

        [Fact]
        public void ShouldAcceptConsecutiveEscapedQuotesInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\"\"\")");
        }
    }
}
