// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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
                                   .Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(" foo(); ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               Factory.MetaCode("}")
                                   .Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void InnerImplicitExpressionWithOnlySingleAtOutputsZeroLengthCodeSpan()
        {
            ParseBlockTest("{@}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharactersInternal.NonWhiteSpace)
                                   ),
                               Factory.EmptyCSharp().AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                           designTime: true,
                           expectedErrors: new[]
                           {
                               RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation(2, 0, 2), contentLength: 1),
                                "}")
                           });
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptDotAfterAt()
        {
            ParseBlockTest("{@.}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharactersInternal.NonWhiteSpace)
                                   ),
                               Factory.Code(".").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                           designTime: true,
                           expectedErrors: new[]
                           {
                               RazorDiagnosticFactory.CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(
                                   new SourceSpan(new SourceLocation(2, 0, 2), contentLength: 1),
                                   ".")
                           });
        }

        [Fact]
        public void InnerImplicitExpressionWithOnlySingleAtAcceptsSingleSpaceOrNewlineAtDesignTime()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(Environment.NewLine + "    ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.EmptyCSharp().AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharactersInternal.NonWhiteSpace)
                                   ),
                               Factory.Code(Environment.NewLine).AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                /* designTimeParser */ true,
                           RazorDiagnosticFactory.CreateParsing_UnexpectedWhiteSpaceAtStartOfCodeBlock(
                                new SourceSpan(new SourceLocation(6 + Environment.NewLine.Length, 1, 5), Environment.NewLine.Length)));
        }

        [Fact]
        public void InnerImplicitExpressionDoesNotAcceptTrailingNewlineInRunTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo.").AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                               Factory.Code(Environment.NewLine).AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void InnerImplicitExpressionAcceptsTrailingNewlineInDesignTimeMode()
        {
            ParseBlockTest("{@foo." + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.EmptyCSharp()
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new ExpressionBlock(
                                   Factory.CodeTransition(),
                                   Factory.Code("foo.").AsImplicitExpression(KeywordSet, acceptTrailingDot: true).Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                               Factory.Code(Environment.NewLine).AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                           designTime: true);
        }
    }
}
