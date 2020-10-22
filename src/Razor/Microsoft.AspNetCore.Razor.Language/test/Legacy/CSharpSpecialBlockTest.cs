// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpSpecialBlockTest : ParserTestBase
    {
        [Fact]
        public void NonKeywordStatementInCodeBlockIsHandledCorrectly()
        {
            ParseDocumentTest(
@"@{
    List<dynamic> photos = gallery.Photo.ToList();
}");
        }

        [Fact]
        public void BalancesBracesOutsideStringsIfFirstCharIsBraceAndReturnsSpanOfTypeCode()
        {
            // ParseBlockBalancesBracesOutsideStringsIfFirstCharacterIsBraceAndReturnsSpanOfTypeCode
            ParseDocumentTest("@{foo\"b}ar\" if(condition) { string.Format(\"{0}\"); } }");
        }

        [Fact]
        public void BalancesParensOutsideStringsIfFirstCharIsParenAndReturnsSpanOfTypeExpr()
        {
            // ParseBlockBalancesParensOutsideStringsIfFirstCharacterIsParenAndReturnsSpanOfTypeExpression
            ParseDocumentTest("@(foo\"b)ar\" if(condition) { string.Format(\"{0}\"); } )");
        }

        [Fact]
        public void ParseBlockIgnoresSingleSlashAtStart()
        {
            ParseDocumentTest("@/ foo");
        }

        [Fact]
        public void ParseBlockTerminatesSingleLineCommentAtEndOfLine()
        {
            ParseDocumentTest(
"@if(!false) {" + Environment.NewLine +
"    // Foo" + Environment.NewLine +
"\t<p>A real tag!</p>" + Environment.NewLine +
"}");
        }
    }
}
