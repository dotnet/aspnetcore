// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class HtmlToCodeSwitchTest : ParserTestBase
{
    [Fact]
    public void SwitchesWhenCharacterBeforeSwapIsNonAlphanumeric()
    {
        ParseDocumentTest("@{<p>foo#@i</p>}");
    }

    [Fact]
    public void SwitchesToCodeWhenSwapCharacterEncounteredMidTag()
    {
        ParseDocumentTest("@{<foo @bar />}");
    }

    [Fact]
    public void SwitchesToCodeWhenSwapCharacterEncounteredInAttributeValue()
    {
        ParseDocumentTest("@{<foo bar=\"@baz\" />}");
    }

    [Fact]
    public void SwitchesToCodeWhenSwapCharacterEncounteredInTagContent()
    {
        ParseDocumentTest("@{<foo>@bar<baz>@boz</baz></foo>}");
    }

    [Fact]
    public void ParsesCodeWithinSingleLineMarkup()
    {
        // TODO: Fix at a later date, HTML should be a tag block: https://github.com/aspnet/Razor/issues/101
        ParseDocumentTest("@{@:<li>Foo @Bar Baz" + Environment.NewLine
                     + "bork}");
    }

    [Fact]
    public void SupportsCodeWithinComment()
    {
        ParseDocumentTest("@{<foo><!-- @foo --></foo>}");
    }

    [Fact]
    public void SupportsCodeWithinSGMLDeclaration()
    {
        ParseDocumentTest("@{<foo><!DOCTYPE foo @bar baz></foo>}");
    }

    [Fact]
    public void SupportsCodeWithinCDataDeclaration()
    {
        ParseDocumentTest("@{<foo><![CDATA[ foo @bar baz]]></foo>}");
    }

    [Fact]
    public void SupportsCodeWithinXMLProcessingInstruction()
    {
        ParseDocumentTest("@{<foo><?xml foo @bar baz?></foo>}");
    }

    [Fact]
    public void DoesNotSwitchToCodeOnEmailAddressInText()
    {
        ParseDocumentTest("@{<foo>anurse@microsoft.com</foo>}");
    }

    [Fact]
    public void DoesNotSwitchToCodeOnEmailAddressInAttribute()
    {
        ParseDocumentTest("@{<a href=\"mailto:anurse@microsoft.com\">Email me</a>}");
    }

    [Fact]
    public void GivesWhitespacePreceedingToCodeIfThereIsNoMarkupOnThatLine()
    {
        ParseDocumentTest("@{   <ul>" + Environment.NewLine
                     + "    @foreach(var p in Products) {" + Environment.NewLine
                     + "        <li>Product: @p.Name</li>" + Environment.NewLine
                     + "    }" + Environment.NewLine
                     + "    </ul>}");
    }

    [Fact]
    public void ParseDocumentGivesWhitespacePreceedingToCodeIfThereIsNoMarkupOnThatLine()
    {
        ParseDocumentTest("   <ul>" + Environment.NewLine
                        + "    @foreach(var p in Products) {" + Environment.NewLine
                        + "        <li>Product: @p.Name</li>" + Environment.NewLine
                        + "    }" + Environment.NewLine
                        + "    </ul>");
    }

    [Fact]
    public void SectionContextGivesWhitespacePreceedingToCodeIfThereIsNoMarkupOnThatLine()
    {
        ParseDocumentTest("@{@section foo {" + Environment.NewLine
                        + "    <ul>" + Environment.NewLine
                        + "        @foreach(var p in Products) {" + Environment.NewLine
                        + "            <li>Product: @p.Name</li>" + Environment.NewLine
                        + "        }" + Environment.NewLine
                        + "    </ul>" + Environment.NewLine
                        + "}}",
            new[] { SectionDirective.Directive, });
    }

    [Fact]
    public void CSharpCodeParserDoesNotAcceptLeadingOrTrailingWhitespaceInDesignMode()
    {
        ParseDocumentTest("@{   <ul>" + Environment.NewLine
                     + "    @foreach(var p in Products) {" + Environment.NewLine
                     + "        <li>Product: @p.Name</li>" + Environment.NewLine
                     + "    }" + Environment.NewLine
                     + "    </ul>}",
            designTime: true);
    }

    // Tests for "@@" escape sequence:
    [Fact]
    public void TreatsTwoAtSignsAsEscapeSequence()
    {
        ParseDocumentTest("@{<foo>@@bar</foo>}");
    }

    [Fact]
    public void TreatsPairsOfAtSignsAsEscapeSequence()
    {
        ParseDocumentTest("@{<foo>@@@@@bar</foo>}");
    }

    [Fact]
    public void ParseDocumentTreatsTwoAtSignsAsEscapeSequence()
    {
        ParseDocumentTest("<foo>@@bar</foo>");
    }

    [Fact]
    public void ParseDocumentTreatsPairsOfAtSignsAsEscapeSequence()
    {
        ParseDocumentTest("<foo>@@@@@bar</foo>");
    }

    [Fact]
    public void SectionBodyTreatsTwoAtSignsAsEscapeSequence()
    {
        ParseDocumentTest(
            "@section Foo { <foo>@@bar</foo> }",
            new[] { SectionDirective.Directive, });
    }

    [Fact]
    public void SectionBodyTreatsPairsOfAtSignsAsEscapeSequence()
    {
        ParseDocumentTest(
            "@section Foo { <foo>@@@@@bar</foo> }",
            new[] { SectionDirective.Directive, });
    }
}
