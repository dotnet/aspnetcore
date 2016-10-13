// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class CSharpTemplateTest : CsHtmlCodeParserTestBase
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
                               Factory.Code(" var foo = ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new TemplateBlock(
                                   new MarkupBlock(
                                       Factory.MarkupTransition(),
                                       Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                       Factory.Markup(" bar" + Environment.NewLine)
                                           .With(new SpanEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString))
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
                               Factory.Code("i")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               new TemplateBlock(
                                   new MarkupBlock(
                                       Factory.MarkupTransition(),
                                       Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                       Factory.Markup(" bar" + Environment.NewLine)
                                           .With(new SpanEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString))
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
                               Factory.Code("foreach(foo in Bar) { Html.ExecuteTemplate(foo, ")
                                   .AsStatement(),
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
                               Factory.Code(" var foo = bar; Html.ExecuteTemplate(foo, ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
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
                               Factory.Code(" var foo = bar; Html.ExecuteTemplate(foo, ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
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
                               Factory.Code(" var foo = bar; Html.ExecuteTemplate(foo, ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               TestNestedTemplate(),
                               Factory.Code("); ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ),
                           GetNestedTemplateError(69));
        }

        [Fact]
        public void ParseBlock_WithDoubleTransition_DoesNotThrow()
        {
            // Arrange
            var testTemplateWithDoubleTransitionCode = " @<p foo='@@'>Foo #@item</p>";
            var testTemplateWithDoubleTransition = new TemplateBlock(
                new MarkupBlock(
                    Factory.MarkupTransition(),
                    new MarkupTagBlock(
                        Factory.Markup("<p"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 46, 0, 46), new LocationTagged<string>("'", 54, 0, 54)),
                            Factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                             new MarkupBlock(
                                Factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 52, 0, 52), new LocationTagged<string>("@", 52, 0, 52))).Accepts(AcceptedCharacters.None),
                                Factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(">").Accepts(AcceptedCharacters.None)),
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

            var expected = new StatementBlock(
                    Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                    Factory.Code(" var foo = bar; Html.ExecuteTemplate(foo, ")
                        .AsStatement()
                        .AutoCompleteWith(autoCompleteString: null),
                    testTemplateWithDoubleTransition,
                    Factory.Code("); ").AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharacters.None));

            // Act & Assert
            ParseBlockTest("{ var foo = bar; Html.ExecuteTemplate(foo," + testTemplateWithDoubleTransitionCode + "); }", expected);
        }

        private static RazorError GetNestedTemplateError(int characterIndex)
        {
            return new RazorError(
                LegacyResources.ParseError_InlineMarkup_Blocks_Cannot_Be_Nested,
                new SourceLocation(characterIndex, 0, characterIndex),
                length: 1);
        }
    }
}
