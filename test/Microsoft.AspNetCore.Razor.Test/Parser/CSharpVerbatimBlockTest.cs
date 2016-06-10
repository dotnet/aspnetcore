// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Parser
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
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
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
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)
                                   ),
                               Factory.EmptyCSharp().AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                           designTimeParser: true,
                           expectedErrors: new[]
                           {
                               new RazorError(
                                   RazorResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS("}"),
                                   new SourceLocation(2, 0, 2),
                                   length: 1)
                           });
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptDotAfterAt()
        {
            ParseBlockTest("{@.}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)
                                   ),
                               Factory.Code(".").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                           designTimeParser: true,
                           expectedErrors: new[]
                           {
                               new RazorError(
                                   RazorResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS("."),
                                   new SourceLocation(2, 0, 2),
                                   length: 1)
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
                               Factory.Code(Environment.NewLine + "    ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)
                                   ),
                               Factory.Code(Environment.NewLine).AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                /* designTimeParser */ true,
                           new RazorError(
                               RazorResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_CS,
                               new SourceLocation(6 + Environment.NewLine.Length, 1, 5),
                               Environment.NewLine.Length));
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptTrailingNewlineInRunTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo.").AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)),
                               Factory.Code(Environment.NewLine).AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void InnerImplicitExpressionAcceptsTrailingNewlineInDesignTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo.").AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharacters.NonWhiteSpace)),
                               Factory.Code(Environment.NewLine).AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)),
                           designTimeParser: true);
        }
    }
}
