// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlAttributeTest : ParserTestBase
    {
        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace1()
        {
            var attributeName = "[item]";
            ParseDocumentTest($"@{{<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace2()
        {
            var attributeName = "[(item,";
            ParseDocumentTest($"@{{<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace3()
        {
            var attributeName = "(click)";
            ParseDocumentTest($"@{{<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace4()
        {
            var attributeName = "(^click)";
            ParseDocumentTest($"@{{<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace5()
        {
            var attributeName = "*something";
            ParseDocumentTest($"@{{<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_BeforeEqualWhitespace6()
        {
            var attributeName = "#local";
            ParseDocumentTest($"@{{<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace1()
        {
            var attributeName = "[item]";
            ParseDocumentTest($"@{{<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace2()
        {
            var attributeName = "[(item,";
            ParseDocumentTest($"@{{<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace3()
        {
            var attributeName = "(click)";
            ParseDocumentTest($"@{{<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace4()
        {
            var attributeName = "(^click)";
            ParseDocumentTest($"@{{<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace5()
        {
            var attributeName = "*something";
            ParseDocumentTest($"@{{<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes_Whitespace6()
        {
            var attributeName = "#local";
            ParseDocumentTest($"@{{<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes1()
        {
            var attributeName = "[item]";
            ParseDocumentTest($"@{{<a {attributeName}='Foo' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes2()
        {
            var attributeName = "[(item,";
            ParseDocumentTest($"@{{<a {attributeName}='Foo' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes3()
        {
            var attributeName = "(click)";
            ParseDocumentTest($"@{{<a {attributeName}='Foo' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes4()
        {
            var attributeName = "(^click)";
            ParseDocumentTest($"@{{<a {attributeName}='Foo' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes5()
        {
            var attributeName = "*something";
            ParseDocumentTest($"@{{<a {attributeName}='Foo' />}}");
        }

        [Fact]
        public void SymbolBoundAttributes6()
        {
            var attributeName = "#local";
            ParseDocumentTest($"@{{<a {attributeName}='Foo' />}}");
        }

        [Fact]
        public void SimpleLiteralAttribute()
        {
            ParseDocumentTest("@{<a href='Foo' />}");
        }

        [Fact]
        public void SimpleLiteralAttributeWithWhitespaceSurroundingEquals()
        {
            ParseDocumentTest("@{<a href \f\r\n= \t\n'Foo' />}");
        }

        [Fact]
        public void DynamicAttributeWithWhitespaceSurroundingEquals()
        {
            ParseDocumentTest("@{<a href \n= \r\n'@Foo' />}");
        }

        [Fact]
        public void MultiPartLiteralAttribute()
        {
            ParseDocumentTest("@{<a href='Foo Bar Baz' />}");
        }

        [Fact]
        public void DoubleQuotedLiteralAttribute()
        {
            ParseDocumentTest("@{<a href=\"Foo Bar Baz\" />}");
        }

        [Fact]
        public void NewLinePrecedingAttribute()
        {
            ParseDocumentTest("@{<a\r\nhref='Foo' />}");
        }

        [Fact]
        public void NewLineBetweenAttributes()
        {
            ParseDocumentTest("@{<a\nhref='Foo'\r\nabcd='Bar' />}");
        }

        [Fact]
        public void WhitespaceAndNewLinePrecedingAttribute()
        {
            ParseDocumentTest("@{<a \t\r\nhref='Foo' />}");
        }

        [Fact]
        public void UnquotedLiteralAttribute()
        {
            ParseDocumentTest("@{<a href=Foo Bar Baz />}");
        }

        [Fact]
        public void SimpleExpressionAttribute()
        {
            ParseDocumentTest("@{<a href='@foo' />}");
        }

        [Fact]
        public void MultiValueExpressionAttribute()
        {
            ParseDocumentTest("@{<a href='@foo bar @baz' />}");
        }

        [Fact]
        public void VirtualPathAttributesWorkWithConditionalAttributes()
        {
            ParseDocumentTest("@{<a href='@foo ~/Foo/Bar' />}");
        }

        [Fact]
        public void UnquotedAttributeWithCodeWithSpacesInBlock()
        {
            ParseDocumentTest("@{<input value=@foo />}");
        }

        [Fact]
        public void UnquotedAttributeWithCodeWithSpacesInDocument()
        {
            ParseDocumentTest("<input value=@foo />}");
        }

        [Fact]
        public void ConditionalAttributesAreEnabledForDataAttributesWithExperimentalFlag()
        {
            ParseDocumentTest(RazorLanguageVersion.Experimental, "@{<span data-foo='@foo'></span>}", directives: null, designTime: false);
        }

        [Fact]
        public void ConditionalAttributesAreDisabledForDataAttributesInBlock()
        {
            ParseDocumentTest("@{<span data-foo='@foo'></span>}");
        }

        [Fact]
        public void ConditionalAttributesWithWeirdSpacingAreDisabledForDataAttributesInBlock()
        {
            ParseDocumentTest("@{<span data-foo  =  '@foo'></span>}");
        }

        [Fact]
        public void ConditionalAttributesAreDisabledForDataAttributesInDocument()
        {
            ParseDocumentTest("@{<span data-foo='@foo'></span>}");
        }

        [Fact]
        public void ConditionalAttributesWithWeirdSpacingAreDisabledForDataAttributesInDocument()
        {
            ParseDocumentTest("@{<span data-foo=@foo ></span>}");
        }

        [Fact]
        public void ComponentFileKind_ParsesDirectiveAttributesAsMarkup()
        {
            ParseDocumentTest("<span @class='@foo'></span>", fileKind: FileKinds.Component);
        }

        [Fact]
        public void ComponentFileKind_ParsesDirectiveAttributesWithParameterAsMarkup()
        {
            ParseDocumentTest("<span @class:param='@foo'></span>", fileKind: FileKinds.Component);
        }
    }
}
