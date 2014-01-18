// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.VB
{
    public class VBErrorTest : VBHtmlCodeParserTestBase
    {
        [Fact]
        public void ParserOutputsErrorAndRecoversToEndOfLineIfExplicitExpressionUnterminated()
        {
            ParseBlockTest(@"(foo
bar",
                new ExpressionBlock(
                    Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                    Factory.Code("foo").AsExpression()),
                new RazorError(
                    String.Format(
                        RazorResources.ParseError_Expected_EndOfBlock_Before_EOF,
                        RazorResources.BlockName_ExplicitExpression,
                        ")", "("),
                    SourceLocation.Zero));
        }

        [Fact]
        public void ParserOutputsZeroLengthCodeSpanIfEofReachedAfterStartOfExplicitExpression()
        {
            ParseBlockTest("(",
                new ExpressionBlock(
                    Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                    Factory.EmptyVB().AsExpression()),
                new RazorError(
                    String.Format(RazorResources.ParseError_Expected_EndOfBlock_Before_EOF, "explicit expression", ")", "("),
                    SourceLocation.Zero));
        }

        [Fact]
        public void ParserOutputsZeroLengthCodeSpanIfEofReachedAfterAtSign()
        {
            ParseBlockTest(String.Empty,
                new ExpressionBlock(
                    Factory.EmptyVB().AsImplicitExpression(KeywordSet).Accepts(AcceptedCharacters.NonWhiteSpace)),
                new RazorError(
                    RazorResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
                    SourceLocation.Zero));
        }

        [Fact]
        public void ParserOutputsZeroLengthCodeSpanIfOnlyWhitespaceFoundAfterAtSign()
        {
            ParseBlockTest(" ",
                new ExpressionBlock(
                    Factory.EmptyVB().AsImplicitExpression(KeywordSet).Accepts(AcceptedCharacters.NonWhiteSpace)),
                new RazorError(
                    RazorResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_VB,
                    SourceLocation.Zero));
        }

        [Fact]
        public void ParserOutputsZeroLengthCodeSpanIfInvalidCharacterFoundAfterAtSign()
        {
            ParseBlockTest("!!!",
                new ExpressionBlock(
                    Factory.EmptyVB().AsImplicitExpression(KeywordSet).Accepts(AcceptedCharacters.NonWhiteSpace)),
                new RazorError(
                    String.Format(RazorResources.ParseError_Unexpected_Character_At_Start_Of_CodeBlock_VB, "!"),
                    SourceLocation.Zero));
        }

        [Theory]
        [InlineData("Code", "End Code", true, true)]
        [InlineData("Do", "Loop", false, false)]
        [InlineData("While", "End While", false, false)]
        [InlineData("If", "End If", false, false)]
        [InlineData("Select Case", "End Select", false, false)]
        [InlineData("For", "Next", false, false)]
        [InlineData("Try", "End Try", false, false)]
        [InlineData("With", "End With", false, false)]
        [InlineData("Using", "End Using", false, false)]
        public void EofBlock(string keyword, string expectedTerminator, bool autoComplete, bool keywordIsMetaCode)
        {
            EofBlockCore(keyword, expectedTerminator, autoComplete, BlockType.Statement, keywordIsMetaCode, c => c.AsStatement());
        }

        [Fact]
        public void EofFunctionsBlock()
        {
            EofBlockCore("Functions", "End Functions", true, BlockType.Functions, true, c => c.AsFunctionsBody());
        }

        private void EofBlockCore(string keyword, string expectedTerminator, bool autoComplete, BlockType blockType, bool keywordIsMetaCode, Func<UnclassifiedCodeSpanConstructor, SpanConstructor> classifier)
        {
            BlockBuilder expected = new BlockBuilder();
            expected.Type = blockType;
            if (keywordIsMetaCode)
            {
                expected.Children.Add(Factory.MetaCode(keyword).Accepts(AcceptedCharacters.None));
                expected.Children.Add(
                classifier(Factory.EmptyVB())
                       .With((SpanEditHandler)(
                            autoComplete ?
                                new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = expectedTerminator } :
                                SpanEditHandler.CreateDefault())));
            }
            else
            {
                expected.Children.Add(
                    classifier(Factory.Code(keyword))
                           .With((SpanEditHandler)(
                                autoComplete ?
                                    new AutoCompleteEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString) { AutoCompleteString = expectedTerminator } :
                                    SpanEditHandler.CreateDefault())));
            }

            ParseBlockTest(keyword,
                expected.Build(),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, keyword, expectedTerminator),
                    SourceLocation.Zero));
        }

        [Theory]
        [InlineData("Code", "End Code", true)]
        [InlineData("Do", "Loop", false)]
        [InlineData("While", "End While", false)]
        [InlineData("If", "End If", false)]
        [InlineData("Select Case", "End Select", false)]
        [InlineData("For", "Next", false)]
        [InlineData("Try", "End Try", false)]
        [InlineData("With", "End With", false)]
        [InlineData("Using", "End Using", false)]
        public void UnterminatedBlock(string keyword, string expectedTerminator, bool keywordIsMetaCode)
        {
            UnterminatedBlockCore(keyword, expectedTerminator, BlockType.Statement, keywordIsMetaCode, c => c.AsStatement());
        }

        [Fact]
        public void UnterminatedFunctionsBlock()
        {
            UnterminatedBlockCore("Functions", "End Functions", BlockType.Functions, true, c => c.AsFunctionsBody());
        }

        private void UnterminatedBlockCore(string keyword, string expectedTerminator, BlockType blockType, bool keywordIsMetaCode, Func<UnclassifiedCodeSpanConstructor, SpanConstructor> classifier)
        {
            const string blockBody = @"
    ' This block is not correctly terminated!";

            BlockBuilder expected = new BlockBuilder();
            expected.Type = blockType;
            if (keywordIsMetaCode)
            {
                expected.Children.Add(Factory.MetaCode(keyword).Accepts(AcceptedCharacters.None));
                expected.Children.Add(classifier(Factory.Code(blockBody)));
            }
            else
            {
                expected.Children.Add(classifier(Factory.Code(keyword + blockBody)));
            }

            ParseBlockTest(keyword + blockBody,
                expected.Build(),
                new RazorError(
                    String.Format(RazorResources.ParseError_BlockNotTerminated, keyword, expectedTerminator),
                    SourceLocation.Zero));
        }
    }
}
