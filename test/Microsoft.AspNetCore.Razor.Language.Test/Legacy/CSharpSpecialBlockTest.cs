// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpSpecialBlockTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void ParseInheritsStatementMarksInheritsSpanAsCanGrowIfMissingTrailingSpace()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(InheritsDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnostic.Create(
                    new RazorError(
                        LegacyResources.FormatUnexpectedEOFAfterDirective(InheritsDirective.Directive.Directive, "type"),
                        new SourceLocation(9, 0, 9), 1)));

            // Act & Assert
            ParseDocumentTest(
                "@inherits",
                new[] { InheritsDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("inherits").Accepts(AcceptedCharactersInternal.None)),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void InheritsBlockAcceptsMultipleGenericArguments()
        {
            ParseDocumentTest(
                "@inherits Foo.Bar<Biz<Qux>, string, int>.Baz",
                new[] { InheritsDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(new DirectiveChunkGenerator(InheritsDirective.Directive),
                        Factory.CodeTransition(),
                        Factory.MetaCode("inherits").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace),
                        Factory.Span(SpanKindInternal.Code, "Foo.Bar<Biz<Qux>, string, int>.Baz", markup: false).AsDirectiveToken(InheritsDirective.Directive.Tokens[0])),
                    Factory.EmptyHtml()));
        }

        [Fact]
        public void InheritsBlockOutputsErrorIfInheritsNotFollowedByTypeButAcceptsEntireLineAsCode()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(InheritsDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnostic.Create(
                    new RazorError(
                        LegacyResources.FormatDirectiveExpectsTypeName(InheritsDirective.Directive.Directive),
                        25, 0, 25, Environment.NewLine.Length)));

            // Act & Assert
            ParseDocumentTest(
                "@inherits                " + Environment.NewLine + "foo",
                new[] { InheritsDirective.Directive },
                new MarkupBlock(
                    Factory.EmptyHtml(),
                    new DirectiveBlock(chunkGenerator,
                        Factory.CodeTransition(),
                        Factory.MetaCode("inherits").Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Code, "                ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.WhiteSpace)),
                    Factory.Markup(Environment.NewLine + "foo")));
        }

        [Fact]
        public void NamespaceImportInsideCodeBlockCausesError()
        {
            ParseBlockTest("{ using Foo.Bar.Baz; var foo = bar; }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(" using Foo.Bar.Baz; var foo = bar; ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)
                               ),
                           new RazorError(
                               LegacyResources.ParseError_NamespaceImportAndTypeAlias_Cannot_Exist_Within_CodeBlock,
                               new SourceLocation(2, 0, 2),
                               length: 5));
        }

        [Fact]
        public void TypeAliasInsideCodeBlockIsNotHandledSpecially()
        {
            ParseBlockTest("{ using Foo = Bar.Baz; var foo = bar; }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(" using Foo = Bar.Baz; var foo = bar; ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)
                               ),
                           new RazorError(
                               LegacyResources.ParseError_NamespaceImportAndTypeAlias_Cannot_Exist_Within_CodeBlock,
                               new SourceLocation(2, 0, 2),
                               length: 5));
        }

        [Fact]
        public void Plan9FunctionsKeywordInsideCodeBlockIsNotHandledSpecially()
        {
            ParseBlockTest("{ functions Foo; }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(" functions Foo; ")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void NonKeywordStatementInCodeBlockIsHandledCorrectly()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    List<dynamic> photos = gallery.Photo.ToList();" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code($"{Environment.NewLine}    List<dynamic> photos = gallery.Photo.ToList();{Environment.NewLine}")
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockBalancesBracesOutsideStringsIfFirstCharacterIsBraceAndReturnsSpanOfTypeCode()
        {
            // Arrange
            const string code = "foo\"b}ar\" if(condition) { string.Format(\"{0}\"); } ";

            // Act/Assert
            ParseBlockTest("{" + code + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(code)
                                   .AsStatement()
                                   .AutoCompleteWith(autoCompleteString: null),
                               Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockBalancesParensOutsideStringsIfFirstCharacterIsParenAndReturnsSpanOfTypeExpression()
        {
            // Arrange
            const string code = "foo\"b)ar\" if(condition) { string.Format(\"{0}\"); } ";

            // Act/Assert
            ParseBlockTest("(" + code + ")",
                           new ExpressionBlock(
                               Factory.MetaCode("(").Accepts(AcceptedCharactersInternal.None),
                               Factory.Code(code).AsExpression(),
                               Factory.MetaCode(")").Accepts(AcceptedCharactersInternal.None)
                               ));
        }

        [Fact]
        public void ParseBlockBalancesBracesAndOutputsContentAsClassLevelCodeSpanIfFirstIdentifierIsFunctionsKeyword()
        {
            const string code = " foo(); \"bar}baz\" ";
            ParseBlockTest(
                "functions {" + code + "} zoop",
                new[] { FunctionsDirective.Directive },
                new DirectiveBlock(new DirectiveChunkGenerator(FunctionsDirective.Directive),
                    Factory.MetaCode("functions").Accepts(AcceptedCharactersInternal.None),
                    Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith(null, atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                    Factory.Code(code).AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ParseBlockDoesNoErrorRecoveryForFunctionsBlock()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(FunctionsDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnostic.Create(
                    new RazorError(
                        LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF("functions", "}", "{"),
                        new SourceLocation(10, 0, 10),
                        length: 1)));

            // Act & Assert
            ParseBlockTest(
                "functions { { { { { } zoop",
                new[] { FunctionsDirective.Directive },
                new DirectiveBlock(chunkGenerator,
                    Factory.MetaCode("functions").Accepts(AcceptedCharactersInternal.None),
                    Factory.Span(SpanKindInternal.Markup, " ", CSharpSymbolType.WhiteSpace).Accepts(AcceptedCharactersInternal.AllWhiteSpace),
                    Factory.MetaCode("{").AutoCompleteWith("}", atEndOfSpan: true).Accepts(AcceptedCharactersInternal.None),
                    Factory.Code(" { { { { } zoop").AsStatement()));
        }

        [Fact]
        public void ParseBlockIgnoresFunctionsUnlessAllLowerCase()
        {
            ParseBlockTest("Functions { foo() }",
                           new ExpressionBlock(
                               Factory.Code("Functions")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)));
        }

        [Fact]
        public void ParseBlockIgnoresSingleSlashAtStart()
        {
            ParseBlockTest("@/ foo",
                           new ExpressionBlock(
                               Factory.CodeTransition(),
                               Factory.EmptyCSharp()
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                           new RazorError(
                               LegacyResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS("/"),
                               new SourceLocation(1, 0, 1),
                               length: 1));
        }

        [Fact]
        public void ParseBlockTerminatesSingleLineCommentAtEndOfLine()
        {
            ParseBlockTest("if(!false) {" + Environment.NewLine
                         + "    // Foo" + Environment.NewLine
                         + "\t<p>A real tag!</p>" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.Code($"if(!false) {{{Environment.NewLine}    // Foo{Environment.NewLine}").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("\t"),
                                   new MarkupTagBlock(
                                       Factory.Markup("<p>").Accepts(AcceptedCharactersInternal.None)),
                                   Factory.Markup("A real tag!"),
                                   new MarkupTagBlock(
                                       Factory.Markup("</p>").Accepts(AcceptedCharactersInternal.None)),
                                   Factory.Markup(Environment.NewLine).Accepts(AcceptedCharactersInternal.None)),
                               Factory.Code("}").AsStatement()
                               ));
        }
    }
}
