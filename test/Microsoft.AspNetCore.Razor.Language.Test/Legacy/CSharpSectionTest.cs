// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpSectionTest : CsHtmlMarkupParserTestBase
    {
        public CSharpSectionTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void ParseSectionBlockCapturesNewlineImmediatelyFollowing()
        {
            ParseDocumentTest(
                "@section" + Environment.NewLine,
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockCapturesWhitespaceToEndOfLineInSectionStatementMissingOpenBrace()
        {
            ParseDocumentTest(
                "@section Foo         " + Environment.NewLine + "    ",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockCapturesWhitespaceToEndOfLineInSectionStatementMissingName()
        {
            ParseDocumentTest(
                "@section         " + Environment.NewLine + "    ",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockIgnoresSectionUnlessAllLowerCase()
        {
            ParseDocumentTest(
                "@Section foo",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndTerminatesSectionBlockIfKeywordNotFollowedByIdentifierStartCharacter()
        {
            ParseDocumentTest(
                "@section 9 { <p>Foo</p> }",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndTerminatesSectionBlockIfNameNotFollowedByOpenBrace()
        {
            ParseDocumentTest(
                "@section foo-bar { <p>Foo</p> }",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParserOutputsErrorOnNestedSections()
        {
            ParseDocumentTest(
                "@section foo { @section bar { <p>Foo</p> } }",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockHandlesEOFAfterOpenBrace()
        {
            ParseDocumentTest(
                "@section foo {",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockHandlesEOFAfterOpenContent1()
        {
            
            ParseDocumentTest(
                "@section foo { ",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockHandlesEOFAfterOpenContent2()
        {

            ParseDocumentTest(
                "@section foo {\n",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockHandlesEOFAfterOpenContent3()
        {

            ParseDocumentTest(
                "@section foo {abc",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockHandlesEOFAfterOpenContent4()
        {

            ParseDocumentTest(
                "@section foo {\n abc",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockHandlesUnterminatedSection()
        {
            ParseDocumentTest(
                "@section foo { <p>Foo{}</p>",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockHandlesUnterminatedSectionWithNestedIf()
        {
            // Arrange
            var newLine = Environment.NewLine;
            var spaces = "    ";

            // Act & Assert
            ParseDocumentTest(
                string.Format(
                    "@section Test{0}{{{0}{1}@if(true){0}{1}{{{0}{1}{1}<p>Hello World</p>{0}{1}}}",
                    newLine,
                    spaces),
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockReportsErrorAndAcceptsWhitespaceToEndOfLineIfSectionNotFollowedByOpenBrace()
        {
            // Arrange
            var chunkGenerator = new DirectiveChunkGenerator(SectionDirective.Directive);
            chunkGenerator.Diagnostics.Add(
                RazorDiagnosticFactory.CreateParsing_UnexpectedEOFAfterDirective(
                    new SourceSpan(new SourceLocation(18 + Environment.NewLine.Length, 1, 0), contentLength: 1),
                    SectionDirective.Directive.Directive,
                    "{"));

            // Act & Assert
            ParseDocumentTest(
                "@section foo      " + Environment.NewLine,
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockAcceptsOpenBraceMultipleLinesBelowSectionName()
        {
            ParseDocumentTest(
                "@section foo      "
                + Environment.NewLine
                + Environment.NewLine
                + Environment.NewLine
                + Environment.NewLine
                + Environment.NewLine
                + Environment.NewLine
                + "{" + Environment.NewLine
                + "<p>Foo</p>" + Environment.NewLine
                + "}",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockParsesNamedSectionCorrectly()
        {
            ParseDocumentTest(
                "@section foo { <p>Foo</p> }",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockDoesNotRequireSpaceBetweenSectionNameAndOpenBrace()
        {
            ParseDocumentTest(
                "@section foo{ <p>Foo</p> }",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockBalancesBraces()
        {
            ParseDocumentTest(
                "@section foo { <script>(function foo() { return 1; })();</script> }",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockAllowsBracesInCSharpExpression()
        {
            ParseDocumentTest(
                "@section foo { I really want to render a close brace, so here I go: @(\"}\") }",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void SectionIsCorrectlyTerminatedWhenCloseBraceImmediatelyFollowsCodeBlock()
        {
            ParseDocumentTest(
                "@section Foo {" + Environment.NewLine
                + "@if(true) {" + Environment.NewLine
                + "}" + Environment.NewLine
                + "}",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void SectionIsCorrectlyTerminatedWhenCloseBraceImmediatelyFollowsCodeBlockNoWhitespace()
        {
            ParseDocumentTest(
                "@section Foo {" + Environment.NewLine
                + "@if(true) {" + Environment.NewLine
                + "}}",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockCorrectlyTerminatesWhenCloseBraceImmediatelyFollowsMarkup()
        {
            ParseDocumentTest(
                "@section foo {something}",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockParsesComment()
        {
            ParseDocumentTest(
                "@section s {<!-- -->}",
                new[] { SectionDirective.Directive });
        }

        // This was a user reported bug (codeplex #710), the section parser wasn't handling
        // comments.
        [Fact]
        public void ParseSectionBlockParsesCommentWithDelimiters()
        {
            ParseDocumentTest(
                "@section s {<!-- > \" '-->}",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockCommentRecoversFromUnclosedTag()
        {
            ParseDocumentTest(
                "@section s {" + Environment.NewLine + "<a" + Environment.NewLine + "<!--  > \" '-->}",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlockParsesXmlProcessingInstruction()
        {
            ParseDocumentTest(
                "@section s { <? xml bleh ?>}",
                new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlock_WithDoubleTransition1()
        {
            ParseDocumentTest("@section s {<span foo='@@' />}", new[] { SectionDirective.Directive });
        }

        [Fact]
        public void ParseSectionBlock_WithDoubleTransition2()
        {
            ParseDocumentTest("@section s {<span foo='@DateTime.Now @@' />}", new[] { SectionDirective.Directive });
        }

    }
}
