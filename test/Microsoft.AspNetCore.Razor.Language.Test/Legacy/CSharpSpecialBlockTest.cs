// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpSpecialBlockTest : CsHtmlCodeParserTestBase
    {
        public CSharpSpecialBlockTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void NamespaceImportInsideCodeBlockCausesError()
        {
            ParseBlockTest("{ using Foo.Bar.Baz; var foo = bar; }");
        }

        [Fact]
        public void TypeAliasInsideCodeBlockIsNotHandledSpecially()
        {
            ParseBlockTest("{ using Foo = Bar.Baz; var foo = bar; }");
        }

        [Fact]
        public void NonKeywordStatementInCodeBlockIsHandledCorrectly()
        {
            ParseBlockTest(
@"{
    List<dynamic> photos = gallery.Photo.ToList();
}");
        }

        [Fact]
        public void ParseBlockBalancesBracesOutsideStringsIfFirstCharacterIsBraceAndReturnsSpanOfTypeCode()
        {
            ParseBlockTest("{foo\"b}ar\" if(condition) { string.Format(\"{0}\"); } }");
        }

        [Fact]
        public void ParseBlockBalancesParensOutsideStringsIfFirstCharacterIsParenAndReturnsSpanOfTypeExpression()
        {
            ParseBlockTest("(foo\"b)ar\" if(condition) { string.Format(\"{0}\"); } )");
        }

        [Fact]
        public void ParseBlockIgnoresSingleSlashAtStart()
        {
            ParseBlockTest("@/ foo");
        }

        [Fact]
        public void ParseBlockTerminatesSingleLineCommentAtEndOfLine()
        {
            ParseBlockTest(
"if(!false) {" + Environment.NewLine +
"    // Foo" + Environment.NewLine +
"\t<p>A real tag!</p>" + Environment.NewLine +
"}");
        }
    }
}
