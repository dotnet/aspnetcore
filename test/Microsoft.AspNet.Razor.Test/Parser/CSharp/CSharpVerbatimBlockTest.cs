// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpVerbatimBlockTest : CsHtmlCodeParserTestBase
    {
        private const string TestExtraKeyword = "model";

        [Fact]
        public void VerbatimBlock()
        {
            ParseBlockTest("@{ foo(); }",
                           new StatementBlock(
                               Factory.CodeTransition(),
                               Factory.MetaCode("{")
                                   .Accepts(AcceptedCharacters.None),
                               Factory.Code(" foo(); ")
                                   .AsStatement(),
                               Factory.MetaCode("}")
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void InnerImplicitExpressionWithOnlySingleAtOutputsZeroLengthCodeSpan()
        {
            ParseBlockTest("{@}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp().AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)
                                   ),
                               Factory.EmptyCSharp().AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                           designTimeParser: true,
                           expectedErrors: new[]
                           {
                               new RazorError(RazorResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS("}"), new SourceLocation(2, 0, 2))
                           });
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptDotAfterAt()
        {
            ParseBlockTest("{@.}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp().AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)
                                   ),
                               Factory.Code(".").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                           designTimeParser: true,
                           expectedErrors: new[]
                           {
                               new RazorError(RazorResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS("."), new SourceLocation(2, 0, 2))
                           });
        }

        [Fact]
        public void InnerImplicitExpressionWithOnlySingleAtAcceptsSingleSpaceOrNewlineAtDesignTime()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code("\r\n    ").AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)
                                   ),
                               Factory.Code("\r\n").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                /* designTimeParser */ true,
                           new RazorError(RazorResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_CS, 8, 1, 5));
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptTrailingNewlineInRunTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp().AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo.").AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)),
                               Factory.Code("\r\n").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void InnerImplicitExpressionAcceptsTrailingNewlineInDesignTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp().AsStatement(),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo.").AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)),
                               Factory.Code("\r\n").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                           designTimeParser: true);
        }
    }
}
