// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
{
    public class CSharpToMarkupSwitchTest : CsHtmlCodeParserTestBase
    {
        [Fact]
        public void SingleAngleBracketDoesNotCauseSwitchIfOuterBlockIsTerminated()
        {
            ParseBlockTest("{ List< }",
                new StatementBlock(
                    Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                    Factory.Code(" List< ").AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnAtTagTemplateTransitionInDesignTimeMode()
        {
            ParseBlockTest("Foo(    @<p>Foo</p>    )",
                           new ExpressionBlock(
                               Factory.Code("Foo(    ")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.Any),
                               new TemplateBlock(
                                   new MarkupBlock(
                                       Factory.MarkupTransition(),
                                       Factory.Markup("<p>Foo</p>").Accepts(AcceptedCharacters.None)
                                       )
                                   ),
                               Factory.Code("    )")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ), designTimeParser: true);
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnAtColonTemplateTransitionInDesignTimeMode()
        {
            ParseBlockTest("Foo(    " + Environment.NewLine
                         + "@:<p>Foo</p>    " + Environment.NewLine
                         + ")",
                           new ExpressionBlock(
                               Factory.Code("Foo(    \r\n").AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                               new TemplateBlock(
                                   new MarkupBlock(
                                       Factory.MarkupTransition(),
                                       Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                       Factory.Markup("<p>Foo</p>    \r\n")
                                           .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                                       )
                                   ),
                               Factory.Code(")")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ), designTimeParser: true);
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnTagTransitionInDesignTimeMode()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    <p>Foo</p>    " + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code("\r\n    ").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("<p>Foo</p>").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("    \r\n").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ), designTimeParser: true);
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnInvalidAtTagTransitionInDesignTimeMode()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @<p>Foo</p>    " + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code("\r\n    ").AsStatement(),
                               new MarkupBlock(
                                   Factory.MarkupTransition(),
                                   Factory.Markup("<p>Foo</p>").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("    \r\n").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ), true,
                           new RazorError(RazorResources.ParseError_AtInCode_Must_Be_Followed_By_Colon_Paren_Or_Identifier_Start, 7, 1, 4));
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnAtColonTransitionInDesignTimeMode()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @:<p>Foo</p>    " + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code("\r\n    ").AsStatement(),
                               new MarkupBlock(
                                   Factory.MarkupTransition(),
                                   Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                   Factory.Markup("<p>Foo</p>    \r\n")
                                       .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                                   ),
                               Factory.EmptyCSharp().AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ), designTimeParser: true);
        }

        [Fact]
        public void ParseBlockShouldSupportSingleLineMarkupContainingStatementBlock()
        {
            ParseBlockTest("Repeat(10," + Environment.NewLine
                         + "    @: @{}" + Environment.NewLine
                         + ")",
                           new ExpressionBlock(
                               Factory.Code("Repeat(10,\r\n    ")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords),
                               new TemplateBlock(
                                   new MarkupBlock(
                                       Factory.MarkupTransition(),
                                       Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                       Factory.Markup(" ")
                                           .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString)),
                                       new StatementBlock(
                                           Factory.CodeTransition(),
                                           Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                                           Factory.EmptyCSharp().AsStatement(),
                                           Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                                           ),
                                       Factory.Markup("\r\n")
                                           .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                                       )
                                   ),
                               Factory.Code(")")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharacters.NonWhiteSpace)
                               ));
        }

        [Fact]
        public void ParseBlockShouldSupportMarkupWithoutPreceedingWhitespace()
        {
            ParseBlockTest("foreach(var file in files){" + Environment.NewLine
                         + Environment.NewLine
                         + Environment.NewLine
                         + "@:Baz" + Environment.NewLine
                         + "<br/>" + Environment.NewLine
                         + "<a>Foo</a>" + Environment.NewLine
                         + "@:Bar" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.Code("foreach(var file in files){\r\n\r\n\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.MarkupTransition(),
                                   Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                   Factory.Markup("Baz\r\n")
                                       .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                                   ),
                               new MarkupBlock(
                                   Factory.Markup("<br/>\r\n")
                                       .Accepts(AcceptedCharacters.None)
                                   ),
                               new MarkupBlock(
                                   Factory.Markup("<a>Foo</a>\r\n")
                                       .Accepts(AcceptedCharacters.None)
                                   ),
                               new MarkupBlock(
                                   Factory.MarkupTransition(),
                                   Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                   Factory.Markup("Bar\r\n")
                                       .With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                                   ),
                               Factory.Code("}").AsStatement().Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockGivesAllWhitespaceOnSameLineExcludingPreceedingNewlineButIncludingTrailingNewLineToMarkup()
        {
            ParseBlockTest("if(foo) {" + Environment.NewLine
                         + "    var foo = \"After this statement there are 10 spaces\";          " + Environment.NewLine
                         + "    <p>" + Environment.NewLine
                         + "        Foo" + Environment.NewLine
                         + "        @bar" + Environment.NewLine
                         + "    </p>" + Environment.NewLine
                         + "    @:Hello!" + Environment.NewLine
                         + "    var biz = boz;" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.Code("if(foo) {\r\n    var foo = \"After this statement there are 10 spaces\";          \r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("    <p>\r\n        Foo\r\n"),
                                   new ExpressionBlock(
                                       Factory.Code("        ").AsStatement(),
                                       Factory.CodeTransition(),
                                       Factory.Code("bar").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)
                                       ),
                                   Factory.Markup("\r\n    </p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               new MarkupBlock(
                                   Factory.Markup("    "),
                                   Factory.MarkupTransition(),
                                   Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                   Factory.Markup("Hello!\r\n").With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                                   ),
                               Factory.Code("    var biz = boz;\r\n}").AsStatement()));
        }

        [Fact]
        public void ParseBlockAllowsMarkupInIfBodyWithBraces()
        {
            ParseBlockTest("if(foo) { <p>Bar</p> } else if(bar) { <p>Baz</p> } else { <p>Boz</p> }",
                           new StatementBlock(
                               Factory.Code("if(foo) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Bar</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} else if(bar) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Baz</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} else {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Boz</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("}").AsStatement().Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockAllowsMarkupInIfBodyWithBracesWithinCodeBlock()
        {
            ParseBlockTest("{ if(foo) { <p>Bar</p> } else if(bar) { <p>Baz</p> } else { <p>Boz</p> } }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" if(foo) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Bar</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} else if(bar) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Baz</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} else {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Boz</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockSupportsMarkupInCaseAndDefaultBranchesOfSwitch()
        {
            // Arrange
            ParseBlockTest("switch(foo) {" + Environment.NewLine
                         + "    case 0:" + Environment.NewLine
                         + "        <p>Foo</p>" + Environment.NewLine
                         + "        break;" + Environment.NewLine
                         + "    case 1:" + Environment.NewLine
                         + "        <p>Bar</p>" + Environment.NewLine
                         + "        return;" + Environment.NewLine
                         + "    case 2:" + Environment.NewLine
                         + "        {" + Environment.NewLine
                         + "            <p>Baz</p>" + Environment.NewLine
                         + "            <p>Boz</p>" + Environment.NewLine
                         + "        }" + Environment.NewLine
                         + "    default:" + Environment.NewLine
                         + "        <p>Biz</p>" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.Code("switch(foo) {\r\n    case 0:\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        <p>Foo</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("        break;\r\n    case 1:\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        <p>Bar</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("        return;\r\n    case 2:\r\n        {\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("            <p>Baz</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               new MarkupBlock(
                                   Factory.Markup("            <p>Boz</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("        }\r\n    default:\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        <p>Biz</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("}").AsStatement().Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsMarkupInCaseAndDefaultBranchesOfSwitchInCodeBlock()
        {
            // Arrange
            ParseBlockTest("{ switch(foo) {" + Environment.NewLine
                         + "    case 0:" + Environment.NewLine
                         + "        <p>Foo</p>" + Environment.NewLine
                         + "        break;" + Environment.NewLine
                         + "    case 1:" + Environment.NewLine
                         + "        <p>Bar</p>" + Environment.NewLine
                         + "        return;" + Environment.NewLine
                         + "    case 2:" + Environment.NewLine
                         + "        {" + Environment.NewLine
                         + "            <p>Baz</p>" + Environment.NewLine
                         + "            <p>Boz</p>" + Environment.NewLine
                         + "        }" + Environment.NewLine
                         + "    default:" + Environment.NewLine
                         + "        <p>Biz</p>" + Environment.NewLine
                         + "} }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" switch(foo) {\r\n    case 0:\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        <p>Foo</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("        break;\r\n    case 1:\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        <p>Bar</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("        return;\r\n    case 2:\r\n        {\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("            <p>Baz</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               new MarkupBlock(
                                   Factory.Markup("            <p>Boz</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("        }\r\n    default:\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        <p>Biz</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnOpenAngleBracket()
        {
            ParseBlockTest("for(int i = 0; i < 10; i++) { <p>Foo</p> }",
                           new StatementBlock(
                               Factory.Code("for(int i = 0; i < 10; i++) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Foo</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("}").AsStatement().Accepts(AcceptedCharacters.None)
                               ));
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnOpenAngleBracketInCodeBlock()
        {
            ParseBlockTest("{ for(int i = 0; i < 10; i++) { <p>Foo</p> } }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" for(int i = 0; i < 10; i++) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" <p>Foo</p> ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnSwitchCharacterFollowedByColon()
        {
            // Arrange
            ParseBlockTest("if(foo) { @:Bar" + Environment.NewLine
                         + "} zoop",
                           new StatementBlock(
                               Factory.Code("if(foo) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" "),
                                   Factory.MarkupTransition(),
                                   Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                   Factory.Markup("Bar\r\n").With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                                   ),
                               Factory.Code("}").AsStatement()));
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnSwitchCharacterFollowedByColonInCodeBlock()
        {
            // Arrange
            ParseBlockTest("{ if(foo) { @:Bar" + Environment.NewLine
                         + "} } zoop",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" if(foo) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" "),
                                   Factory.MarkupTransition(),
                                   Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                   Factory.Markup("Bar\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockCorrectlyReturnsFromMarkupBlockWithPseudoTag()
        {
            ParseBlockTest("if (i > 0) { <text>;</text> }",
                           new StatementBlock(
                               Factory.Code("if (i > 0) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" "),
                                   Factory.MarkupTransition("<text>").Accepts(AcceptedCharacters.None),
                                   Factory.Markup(";"),
                                   Factory.MarkupTransition("</text>").Accepts(AcceptedCharacters.None),
                                   Factory.Markup(" ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("}").AsStatement()));
        }

        [Fact]
        public void ParseBlockCorrectlyReturnsFromMarkupBlockWithPseudoTagInCodeBlock()
        {
            ParseBlockTest("{ if (i > 0) { <text>;</text> } }",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code(" if (i > 0) {").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup(" "),
                                   Factory.MarkupTransition("<text>").Accepts(AcceptedCharacters.None),
                                   Factory.Markup(";"),
                                   Factory.MarkupTransition("</text>").Accepts(AcceptedCharacters.None),
                                   Factory.Markup(" ").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("} ").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ParseBlockSupportsAllKindsOfImplicitMarkupInCodeBlock()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    if(true) {" + Environment.NewLine
                         + "        @:Single Line Markup" + Environment.NewLine
                         + "    }" + Environment.NewLine
                         + "    foreach (var p in Enumerable.Range(1, 10)) {" + Environment.NewLine
                         + "        <text>The number is @p</text>" + Environment.NewLine
                         + "    }" + Environment.NewLine
                         + "    if(!false) {" + Environment.NewLine
                         + "        <p>A real tag!</p>" + Environment.NewLine
                         + "    }" + Environment.NewLine
                         + "}",
                           new StatementBlock(
                               Factory.MetaCode("{").Accepts(AcceptedCharacters.None),
                               Factory.Code("\r\n    if(true) {\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        "),
                                   Factory.MarkupTransition(),
                                   Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                                   Factory.Markup("Single Line Markup\r\n").With(new SingleLineMarkupEditHandler(CSharpLanguageCharacteristics.Instance.TokenizeString, AcceptedCharacters.None))
                                   ),
                               Factory.Code("    }\r\n    foreach (var p in Enumerable.Range(1, 10)) {\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        "),
                                   Factory.MarkupTransition("<text>").Accepts(AcceptedCharacters.None),
                                   Factory.Markup("The number is "),
                                   new ExpressionBlock(
                                       Factory.CodeTransition(),
                                       Factory.Code("p").AsImplicitExpression(CSharpCodeParser.DefaultKeywords).Accepts(AcceptedCharacters.NonWhiteSpace)
                                       ),
                                   Factory.MarkupTransition("</text>").Accepts(AcceptedCharacters.None),
                                   Factory.Markup("\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("    }\r\n    if(!false) {\r\n").AsStatement(),
                               new MarkupBlock(
                                   Factory.Markup("        <p>A real tag!</p>\r\n").Accepts(AcceptedCharacters.None)
                                   ),
                               Factory.Code("    }\r\n").AsStatement(),
                               Factory.MetaCode("}").Accepts(AcceptedCharacters.None)));
        }
    }
}
