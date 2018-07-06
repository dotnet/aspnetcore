// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpExplicitExpressionTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseBlockShouldOutputZeroLengthCodeSpanIfExplicitExpressionIsEmpty()
        {
            ParseBlockTest("@()");
        }

        [Fact]
        public void ParseBlockShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpression()
        {
            ParseBlockTest("@(");
        }

        [Fact]
        public void ParseBlockShouldAcceptEscapedQuoteInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"\")");
        }

        [Fact]
        public void ParseBlockShouldAcceptEscapedQuoteInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\")");
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleRepeatedEscapedQuoteInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\"\"\")");
        }

        [Fact]
        public void ParseBlockShouldAcceptMultiLineVerbatimStrings()
        {
            ParseBlockTest(@"@(@""" + Environment.NewLine
                         + @"Foo" + Environment.NewLine
                         + @"Bar" + Environment.NewLine
                         + @"Baz" + Environment.NewLine
                         + @""")");
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleEscapedQuotesInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"hello, world\\\"\")");
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleEscapedQuotesInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"hello, world\"\"\")");
        }

        [Fact]
        public void ParseBlockShouldAcceptConsecutiveEscapedQuotesInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"\\\"\")");
        }

        [Fact]
        public void ParseBlockShouldAcceptConsecutiveEscapedQuotesInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\"\"\")");
        }
    }
}
