// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlAttributeTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace1()
        {
            var attributeName = "[item]";
            ParseBlockTest($"<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace2()
        {
            var attributeName = "[(item,";
            ParseBlockTest($"<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace3()
        {
            var attributeName = "(click)";
            ParseBlockTest($"<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace4()
        {
            var attributeName = "(^click)";
            ParseBlockTest($"<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace5()
        {
            var attributeName = "*something";
            ParseBlockTest($"<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace6()
        {
            var attributeName = "#local";
            ParseBlockTest($"<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace1()
        {
            var attributeName = "[item]";
            ParseBlockTest($"<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace2()
        {
            var attributeName = "[(item,";
            ParseBlockTest($"<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace3()
        {
            var attributeName = "(click)";
            ParseBlockTest($"<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace4()
        {
            var attributeName = "(^click)";
            ParseBlockTest($"<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace5()
        {
            var attributeName = "*something";
            ParseBlockTest($"<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace6()
        {
            var attributeName = "#local";
            ParseBlockTest($"<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />");
        }

        [Fact]
        public void SymbolBoundAttributes1()
        {
            var attributeName = "[item]";
            ParseBlockTest($"<a {attributeName}='Foo' />");
        }

        [Fact]
        public void SymbolBoundAttributes2()
        {
            var attributeName = "[(item,";
            ParseBlockTest($"<a {attributeName}='Foo' />");
        }

        [Fact]
        public void SymbolBoundAttributes3()
        {
            var attributeName = "(click)";
            ParseBlockTest($"<a {attributeName}='Foo' />");
        }

        [Fact]
        public void SymbolBoundAttributes4()
        {
            var attributeName = "(^click)";
            ParseBlockTest($"<a {attributeName}='Foo' />");
        }

        [Fact]
        public void SymbolBoundAttributes5()
        {
            var attributeName = "*something";
            ParseBlockTest($"<a {attributeName}='Foo' />");
        }

        [Fact]
        public void SymbolBoundAttributes6()
        {
            var attributeName = "#local";
            ParseBlockTest($"<a {attributeName}='Foo' />");
        }

        [Fact]
        public void SimpleLiteralAttribute()
        {
            ParseBlockTest("<a href='Foo' />");
        }

        [Fact]
        public void SimpleLiteralAttributeWithWhitespaceSurroundingEquals()
        {
            ParseBlockTest("<a href \f\r\n= \t\n'Foo' />");
        }

        [Fact]
        public void DynamicAttributeWithWhitespaceSurroundingEquals()
        {
            ParseBlockTest("<a href \n= \r\n'@Foo' />");
        }

        [Fact]
        public void MultiPartLiteralAttribute()
        {
            ParseBlockTest("<a href='Foo Bar Baz' />");
        }

        [Fact]
        public void DoubleQuotedLiteralAttribute()
        {
            ParseBlockTest("<a href=\"Foo Bar Baz\" />");
        }

        [Fact]
        public void NewLinePrecedingAttribute()
        {
            ParseBlockTest("<a\r\nhref='Foo' />");
        }

        [Fact]
        public void NewLineBetweenAttributes()
        {
            ParseBlockTest("<a\nhref='Foo'\r\nabcd='Bar' />");
        }

        [Fact]
        public void WhitespaceAndNewLinePrecedingAttribute()
        {
            ParseBlockTest("<a \t\r\nhref='Foo' />");
        }

        [Fact]
        public void UnquotedLiteralAttribute()
        {
            ParseBlockTest("<a href=Foo Bar Baz />");
        }

        [Fact]
        public void SimpleExpressionAttribute()
        {
            ParseBlockTest("<a href='@foo' />");
        }

        [Fact]
        public void MultiValueExpressionAttribute()
        {
            ParseBlockTest("<a href='@foo bar @baz' />");
        }

        [Fact]
        public void VirtualPathAttributesWorkWithConditionalAttributes()
        {
            ParseBlockTest("<a href='@foo ~/Foo/Bar' />");
        }

        [Fact]
        public void UnquotedAttributeWithCodeWithSpacesInBlock()
        {
            ParseBlockTest("<input value=@foo />");
        }

        [Fact]
        public void UnquotedAttributeWithCodeWithSpacesInDocument()
        {
            ParseDocumentTest("<input value=@foo />");
        }

        [Fact]
        public void ConditionalAttributesAreEnabledForDataAttributesWithExperimentalFlag()
        {
            ParseBlockTest(
                RazorLanguageVersion.Experimental,
                "<span data-foo='@foo'></span>");
        }

        [Fact]
        public void ConditionalAttributesAreDisabledForDataAttributesInBlock()
        {
            ParseBlockTest("<span data-foo='@foo'></span>");
        }

        [Fact]
        public void ConditionalAttributesWithWeirdSpacingAreDisabledForDataAttributesInBlock()
        {
            ParseBlockTest("<span data-foo  =  '@foo'></span>");
        }

        [Fact]
        public void ConditionalAttributesAreDisabledForDataAttributesInDocument()
        {
            ParseDocumentTest("<span data-foo='@foo'></span>");
        }

        [Fact]
        public void ConditionalAttributesWithWeirdSpacingAreDisabledForDataAttributesInDocument()
        {
            ParseDocumentTest("<span data-foo=@foo ></span>");
        }
    }
}
