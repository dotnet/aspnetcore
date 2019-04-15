// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class CSharpToMarkupSwitchTest : ParserTestBase
    {
        [Fact]
        public void SingleAngleBracketDoesNotCauseSwitchIfOuterBlockIsTerminated()
        {
            ParseDocumentTest("@{ List< }");
        }

        [Fact]
        public void GivesSpacesToCodeOnAtTagTemplateTransitionInDesignTimeMode()
        {
            ParseDocumentTest("@Foo(    @<p>Foo</p>    )", designTime: true);
        }

        [Fact]
        public void GivesSpacesToCodeOnAtColonTemplateTransitionInDesignTimeMode()
        {
            ParseDocumentTest("@Foo(    " + Environment.NewLine
                         + "@:<p>Foo</p>    " + Environment.NewLine
                         + ")", designTime: true);
        }

        [Fact]
        public void GivesSpacesToCodeOnTagTransitionInDesignTimeMode()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                         + "    <p>Foo</p>    " + Environment.NewLine
                         + "}", designTime: true);
        }

        [Fact]
        public void GivesSpacesToCodeOnInvalidAtTagTransitionInDesignTimeMode()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                         + "    @<p>Foo</p>    " + Environment.NewLine
                         + "}", designTime: true);
        }

        [Fact]
        public void GivesSpacesToCodeOnAtColonTransitionInDesignTimeMode()
        {
            ParseDocumentTest("@{" + Environment.NewLine
                         + "    @:<p>Foo</p>    " + Environment.NewLine
                         + "}", designTime: true);
        }

        [Fact]
        public void ShouldSupportSingleLineMarkupContainingStatementBlock()
        {
            ParseDocumentTest("@Repeat(10," + Environment.NewLine
                         + "    @: @{}" + Environment.NewLine
                         + ")");
        }

        [Fact]
        public void ShouldSupportMarkupWithoutPreceedingWhitespace()
        {
            ParseDocumentTest("@foreach(var file in files){" + Environment.NewLine
                         + Environment.NewLine
                         + Environment.NewLine
                         + "@:Baz" + Environment.NewLine
                         + "<br/>" + Environment.NewLine
                         + "<a>Foo</a>" + Environment.NewLine
                         + "@:Bar" + Environment.NewLine
                         + "}");
        }

        [Fact]
        public void GivesAllWhitespaceOnSameLineWithTrailingNewLineToMarkupExclPreceedingNewline()
        {
            // ParseBlockGivesAllWhitespaceOnSameLineExcludingPreceedingNewlineButIncludingTrailingNewLineToMarkup
            ParseDocumentTest("@if(foo) {" + Environment.NewLine
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
        public void AllowsMarkupInIfBodyWithBraces()
        {
            ParseDocumentTest("@if(foo) { <p>Bar</p> } else if(bar) { <p>Baz</p> } else { <p>Boz</p> }");
        }

        [Fact]
        public void AllowsMarkupInIfBodyWithBracesWithinCodeBlock()
        {
            ParseDocumentTest("@{ if(foo) { <p>Bar</p> } else if(bar) { <p>Baz</p> } else { <p>Boz</p> } }");
        }

        [Fact]
        public void SupportsMarkupInCaseAndDefaultBranchesOfSwitch()
        {
            // Arrange
            ParseDocumentTest("@switch(foo) {" + Environment.NewLine
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
        public void SupportsMarkupInCaseAndDefaultBranchesOfSwitchInCodeBlock()
        {
            // Arrange
            ParseDocumentTest("@{ switch(foo) {" + Environment.NewLine
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
        public void ParsesMarkupStatementOnOpenAngleBracket()
        {
            ParseDocumentTest("@for(int i = 0; i < 10; i++) { <p>Foo</p> }");
        }

        [Fact]
        public void ParsesMarkupStatementOnOpenAngleBracketInCodeBlock()
        {
            ParseDocumentTest("@{ for(int i = 0; i < 10; i++) { <p>Foo</p> } }");
        }

        [Fact]
        public void ParsesMarkupStatementOnSwitchCharacterFollowedByColon()
        {
            // Arrange
            ParseDocumentTest("@if(foo) { @:Bar" + Environment.NewLine
                         + "} zoop");
        }

        [Fact]
        public void ParsesMarkupStatementOnSwitchCharacterFollowedByDoubleColon()
        {
            // Arrange
            ParseDocumentTest("@if(foo) { @::Sometext" + Environment.NewLine
                         + "}");
        }


        [Fact]
        public void ParsesMarkupStatementOnSwitchCharacterFollowedByTripleColon()
        {
            // Arrange
            ParseDocumentTest("@if(foo) { @:::Sometext" + Environment.NewLine
                         + "}");
        }

        [Fact]
        public void ParsesMarkupStatementOnSwitchCharacterFollowedByColonInCodeBlock()
        {
            // Arrange
            ParseDocumentTest("@{ if(foo) { @:Bar" + Environment.NewLine
                         + "} } zoop");
        }

        [Fact]
        public void CorrectlyReturnsFromMarkupBlockWithPseudoTag()
        {
            ParseDocumentTest("@if (i > 0) { <text>;</text> }");
        }

        [Fact]
        public void CorrectlyReturnsFromMarkupBlockWithPseudoTagInCodeBlock()
        {
            ParseDocumentTest("@{ if (i > 0) { <text>;</text> } }");
        }

        [Fact]
        public void SupportsAllKindsOfImplicitMarkupInCodeBlock()
        {
            ParseDocumentTest("@{" + Environment.NewLine
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
