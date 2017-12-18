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
            ParseBlockTest("@()",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.EmptyCSharp().AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpression()
        {
            ParseBlockTest("@(",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.EmptyCSharp().AsExpression()
                               ),
                           RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                               new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1),
                               LegacyResources.BlockName_ExplicitExpression,
                               ")",
                               "("));
        }

        [Fact]
        public void ParseBlockShouldAcceptEscapedQuoteInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code("\"\\\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptEscapedQuoteInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code("@\"\"\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleRepeatedEscapedQuoteInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\"\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code("@\"\"\"\"\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptMultiLineVerbatimStrings()
        {
            ParseBlockTest(@"@(@""" + Environment.NewLine
                         + @"Foo" + Environment.NewLine
                         + @"Bar" + Environment.NewLine
                         + @"Baz" + Environment.NewLine
                         + @""")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code($"@\"{Environment.NewLine}Foo{Environment.NewLine}Bar{Environment.NewLine}Baz{Environment.NewLine}\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleEscapedQuotesInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"hello, world\\\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code("\"\\\"hello, world\\\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleEscapedQuotesInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"hello, world\"\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code("@\"\"\"hello, world\"\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptConsecutiveEscapedQuotesInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"\\\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code("\"\\\"\\\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptConsecutiveEscapedQuotesInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\"\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code("@\"\"\"\"\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }
    }
}
