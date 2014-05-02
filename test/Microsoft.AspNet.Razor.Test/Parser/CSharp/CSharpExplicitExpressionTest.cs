// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpExplicitExpressionTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseBlockShouldOutputZeroLengthCodeSpanIfExplicitExpressionIsEmpty()
        {
            ParseBlockTest("@()",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp().AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldOutputZeroLengthCodeSpanIfEOFOccursAfterStartOfExplicitExpression()
        {
            ParseBlockTest("@(",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp().AsExpression()
                               ),
                           new RazorError(
                                    RazorResources.ParseError_Expected_EndOfBlock_Before_EOF(RazorResources.BlockName_ExplicitExpression, ")", "("),
                               new SourceLocation(1, 0, 1)));
        }

        [Fact]
        public void ParseBlockShouldAcceptEscapedQuoteInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("\"\\\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptEscapedQuoteInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("@\"\"\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleRepeatedEscapedQuoteInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\"\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("@\"\"\"\"\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
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
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("@\"\r\nFoo\r\nBar\r\nBaz\r\n\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleEscapedQuotesInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"hello, world\\\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("\"\\\"hello, world\\\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptMultipleEscapedQuotesInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"hello, world\"\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("@\"\"\"hello, world\"\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptConsecutiveEscapedQuotesInNonVerbatimStrings()
        {
            ParseBlockTest("@(\"\\\"\\\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("\"\\\"\\\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockShouldAcceptConsecutiveEscapedQuotesInVerbatimStrings()
        {
            ParseBlockTest("@(@\"\"\"\"\"\")",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("@\"\"\"\"\"\"").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }
    }
}
