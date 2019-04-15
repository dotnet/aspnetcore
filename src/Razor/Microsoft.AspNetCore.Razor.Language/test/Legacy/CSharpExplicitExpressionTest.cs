// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpExplicitExpressionTest : ParserTestBase
    {
        [Fact]
        public void ShouldOutputZeroLengthCodeSpanIfExplicitExpressionIsEmpty()
        {
            ParseDocumentTest("@()");
        }

        [Fact]
        public void ShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpr()
        {
            // ParseBlockShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpression
            ParseDocumentTest("@(");
        }

        [Fact]
        public void ShouldAcceptEscapedQuoteInNonVerbatimStrings()
        {
            ParseDocumentTest("@(\"\\\"\")");
        }

        [Fact]
        public void ShouldAcceptEscapedQuoteInVerbatimStrings()
        {
            ParseDocumentTest("@(@\"\"\"\")");
        }

        [Fact]
        public void ShouldAcceptMultipleRepeatedEscapedQuoteInVerbatimStrings()
        {
            ParseDocumentTest("@(@\"\"\"\"\"\")");
        }

        [Fact]
        public void ShouldAcceptMultiLineVerbatimStrings()
        {
            ParseDocumentTest(@"@(@""" + Environment.NewLine
                         + @"Foo" + Environment.NewLine
                         + @"Bar" + Environment.NewLine
                         + @"Baz" + Environment.NewLine
                         + @""")");
        }

        [Fact]
        public void ShouldAcceptMultipleEscapedQuotesInNonVerbatimStrings()
        {
            ParseDocumentTest("@(\"\\\"hello, world\\\"\")");
        }

        [Fact]
        public void ShouldAcceptMultipleEscapedQuotesInVerbatimStrings()
        {
            ParseDocumentTest("@(@\"\"\"hello, world\"\"\")");
        }

        [Fact]
        public void ShouldAcceptConsecutiveEscapedQuotesInNonVerbatimStrings()
        {
            ParseDocumentTest("@(\"\\\"\\\"\")");
        }

        [Fact]
        public void ShouldAcceptConsecutiveEscapedQuotesInVerbatimStrings()
        {
            ParseDocumentTest("@(@\"\"\"\"\"\")");
        }
    }
}
