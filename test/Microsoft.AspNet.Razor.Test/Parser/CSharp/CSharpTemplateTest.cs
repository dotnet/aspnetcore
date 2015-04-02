// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpTemplateTest : CsHtmlCodeParserTestBase
    {
        private const string TestTemplateCode = " @<p>Foo #@item</p>";

        private TemplateBlock TestTemplate()
        {
            return new TemplateBlock(
                new MarkupBlock(
                    Factory.MarkupTransition(),
                    new MarkupTagBlock(
                        Factory.Markup("<p>").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("Foo #"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("item")
                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                            .Accepts(AcceptedCharacters.NonWhiteSpace)
                        ),
                    new MarkupTagBlock(
                        Factory.Markup("</p>").Accepts(AcceptedCharacters.None))
                    )
                );
        }

        private const string TestNestedTemplateCode = " @<p>Foo #@Html.Repeat(10, @<p>@item</p>)</p>";

        private TemplateBlock TestNestedTemplate()
        {
            return new TemplateBlock(
                new MarkupBlock(
                    Factory.MarkupTransition(),
                    new MarkupTagBlock(
                        Factory.Markup("<p>").Accepts(AcceptedCharacters.None)),
                    Factory.Markup("Foo #"),
                    new ExpressionBlock(
                        Factory.CodeTransition(),
                        Factory.Code("Html.Repeat(10, ")
                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                        new TemplateBlock(
                            new MarkupBlock(
                                Factory.MarkupTransition(),
                                new MarkupTagBlock(
                                    Factory.Markup("<p>").Accepts(AcceptedCharacters.None)),
                                Factory.EmptyHtml(),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("item")
                                        .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                        .Accepts(AcceptedCharacters.NonWhiteSpace)
                                    ),
                                new MarkupTagBlock(
                                    Factory.Markup("</p>").Accepts(AcceptedCharacters.None))
                                )
                            ),
                        Factory.Code(")")
                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                            .Accepts(AcceptedCharacters.NonWhiteSpace)
                        ),
                    new MarkupTagBlock(
                        Factory.Markup("</p>").Accepts(AcceptedCharacters.None))
                    )
                );
        }

        [Fact]
        public void ParseBlockHandlesSingleLineTemplate()
        {
            ParseBlockTest("{ var foo = @: bar" + Environment.NewLine
                         + "; }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" var foo = ").AsStatement(),
                               new TemplateBlock(
                                   new MarkupBlock(
                                       Factory.MarkupTransition(),
                                       Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                       Factory.Markup(" bar" + Environment.NewLine)
                                           .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString))
                                           .Accepts(AcceptedCharacters.None)
                                       )
                                   ),
                               Factory.Code("; ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockHandlesSingleLineImmediatelyFollowingStatementChar()
        {
            ParseBlockTest("{i@: bar" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code("i").AsStatement(),
                               new TemplateBlock(
                                   new MarkupBlock(
                                       Factory.MarkupTransition(),
                                       Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                       Factory.Markup(" bar" + Environment.NewLine)
                                           .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString))
                                           .Accepts(AcceptedCharacters.None)
                                       )
                                   ),
                               Factory.EmptyCSharp().AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockHandlesSimpleTemplateInExplicitExpressionParens()
        {
            ParseBlockTest("(Html.Repeat(10," + TestTemplateCode + "))",
                           new ExpressionBlock(
                               Factory.MetaCode("(").Accepts(AcceptedCharacters.None),
                               Factory.Code("Html.Repeat(10, ").AsExpression(),
                               TestTemplate(),
                               Factory.Code(")").AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockHandlesSimpleTemplateInImplicitExpressionParens()
        {
            ParseBlockTest("Html.Repeat(10," + TestTemplateCode + ")",
                           new ExpressionBlock(
                               Factory.Code("Html.Repeat(10, ")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                               TestTemplate(),
                               Factory.Code(")")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ));
        }

        [Fact]
        public void ParseBlockHandlesTwoTemplatesInImplicitExpressionParens()
        {
            ParseBlockTest("Html.Repeat(10," + TestTemplateCode + "," + TestTemplateCode + ")",
                           new ExpressionBlock(
                               Factory.Code("Html.Repeat(10, ")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                               TestTemplate(),
                               Factory.Code(", ")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                               TestTemplate(),
                               Factory.Code(")")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)
                               ));
        }

        [Fact]
        public void ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInImplicitExpressionParens()
        {
            ParseBlockTest("Html.Repeat(10," + TestNestedTemplateCode + ")",
                           new ExpressionBlock(
                               Factory.Code("Html.Repeat(10, ")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                               TestNestedTemplate(),
                               Factory.Code(")")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ),
                           GetNestedTemplateError(42));
        }

        [Fact]
        public void ParseBlockHandlesSimpleTemplateInStatementWithinCodeBlock()
        {
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo," + TestTemplateCode + "); }",
                           new StatementBlock(
                               Factory.Code("foreach(foo in Bar) { Html.ExecuteTemplate(foo, ").AsStatement(),
                               TestTemplate(),
                               Factory.Code("); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockHandlesTwoTemplatesInStatementWithinCodeBlock()
        {
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo," + TestTemplateCode + "," + TestTemplateCode + "); }",
                           new StatementBlock(
                               Factory.Code("foreach(foo in Bar) { Html.ExecuteTemplate(foo, ").AsStatement(),
                               TestTemplate(),
                               Factory.Code(", ").AsStatement(),
                               TestTemplate(),
                               Factory.Code("); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInStatementWithinCodeBlock()
        {
            ParseBlockTest("foreach(foo in Bar) { Html.ExecuteTemplate(foo," + TestNestedTemplateCode + "); }",
                           new StatementBlock(
                               Factory.Code("foreach(foo in Bar) { Html.ExecuteTemplate(foo, ").AsStatement(),
                               TestNestedTemplate(),
                               Factory.Code("); }")
                                   .AsStatement()
                                   .Accepts(AcceptedCharacters.None)
                               ),
                           GetNestedTemplateError(74));
        }

        [Fact]
        public void ParseBlockHandlesSimpleTemplateInStatementWithinStatementBlock()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo," + TestTemplateCode + "); }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" var foo = bar; Html.ExecuteTemplate(foo, ").AsStatement(),
                               TestTemplate(),
                               Factory.Code("); ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockHandlessTwoTemplatesInStatementWithinStatementBlock()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo," + TestTemplateCode + "," + TestTemplateCode + "); }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" var foo = bar; Html.ExecuteTemplate(foo, ").AsStatement(),
                               TestTemplate(),
                               Factory.Code(", ").AsStatement(),
                               TestTemplate(),
                               Factory.Code("); ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockProducesErrorButCorrectlyParsesNestedTemplateInStatementWithinStatementBlock()
        {
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo," + TestNestedTemplateCode + "); }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" var foo = bar; Html.ExecuteTemplate(foo, ").AsStatement(),
                               TestNestedTemplate(),
                               Factory.Code("); ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ),
                           GetNestedTemplateError(69));
        }

        private static RazorError GetNestedTemplateError(int characterIndex)
        {
            return new RazorError(RazorResources.ParseError_InlineMarkup_Blocks_Cannot_Be_Nested, new SourceLocation(characterIndex, 0, characterIndex));
        }
    }
}
