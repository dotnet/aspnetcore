// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpToMarkupSwitchTest : CsHtmlCodeParserTestBase
    {
        public CSharpToMarkupSwitchTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void SingleAngleBracketDoesNotCauseSwitchIfOuterBlockIsTerminated()
        {
            ParseBlockTest("{ List< }");
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnAtTagTemplateTransitionInDesignTimeMode()
        {
            ParseBlockTest("Foo(    @<p>Foo</p>    )", designTime: true);
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnAtColonTemplateTransitionInDesignTimeMode()
        {
            ParseBlockTest("Foo(    " + Environment.NewLine
                         + "@:<p>Foo</p>    " + Environment.NewLine
                         + ")", designTime: true);
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnTagTransitionInDesignTimeMode()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    <p>Foo</p>    " + Environment.NewLine
                         + "}", designTime: true);
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnInvalidAtTagTransitionInDesignTimeMode()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @<p>Foo</p>    " + Environment.NewLine
                         + "}", designTime: true);
        }

        [Fact]
        public void ParseBlockGivesSpacesToCodeOnAtColonTransitionInDesignTimeMode()
        {
            ParseBlockTest("{" + Environment.NewLine
                         + "    @:<p>Foo</p>    " + Environment.NewLine
                         + "}", designTime: true);
        }

        [Fact]
        public void ParseBlockShouldSupportSingleLineMarkupContainingStatementBlock()
        {
            ParseBlockTest("Repeat(10," + Environment.NewLine
                         + "    @: @{}" + Environment.NewLine
                         + ")");
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
                         + "}");
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
                         + "}");
        }

        [Fact]
        public void ParseBlockAllowsMarkupInIfBodyWithBraces()
        {
            ParseBlockTest("if(foo) { <p>Bar</p> } else if(bar) { <p>Baz</p> } else { <p>Boz</p> }");
        }

        [Fact]
        public void ParseBlockAllowsMarkupInIfBodyWithBracesWithinCodeBlock()
        {
            ParseBlockTest("{ if(foo) { <p>Bar</p> } else if(bar) { <p>Baz</p> } else { <p>Boz</p> } }");
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
                         + "}");
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
                         + "} }");
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnOpenAngleBracket()
        {
            ParseBlockTest("for(int i = 0; i < 10; i++) { <p>Foo</p> }");
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnOpenAngleBracketInCodeBlock()
        {
            ParseBlockTest("{ for(int i = 0; i < 10; i++) { <p>Foo</p> } }");
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnSwitchCharacterFollowedByColon()
        {
            // Arrange
            ParseBlockTest("if(foo) { @:Bar" + Environment.NewLine
                         + "} zoop");
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnSwitchCharacterFollowedByDoubleColon()
        {
            // Arrange
            ParseBlockTest("if(foo) { @::Sometext" + Environment.NewLine
                         + "}");
        }


        [Fact]
        public void ParseBlockParsesMarkupStatementOnSwitchCharacterFollowedByTripleColon()
        {
            // Arrange
            ParseBlockTest("if(foo) { @:::Sometext" + Environment.NewLine
                         + "}");
        }

        [Fact]
        public void ParseBlockParsesMarkupStatementOnSwitchCharacterFollowedByColonInCodeBlock()
        {
            // Arrange
            ParseBlockTest("{ if(foo) { @:Bar" + Environment.NewLine
                         + "} } zoop");
        }

        [Fact]
        public void ParseBlockCorrectlyReturnsFromMarkupBlockWithPseudoTag()
        {
            ParseBlockTest("if (i > 0) { <text>;</text> }");
        }

        [Fact]
        public void ParseBlockCorrectlyReturnsFromMarkupBlockWithPseudoTagInCodeBlock()
        {
            ParseBlockTest("{ if (i > 0) { <text>;</text> } }");
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
                         + "}");
        }
    }
}
