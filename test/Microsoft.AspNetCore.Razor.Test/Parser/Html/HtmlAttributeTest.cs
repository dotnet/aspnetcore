// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Microsoft.AspNetCore.Razor.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.Parser.Html
{
    public class HtmlAttributeTest : CsHtmlMarkupParserTestBase
    {
        public static TheoryData SymbolBoundAttributeNames
        {
            get
            {
                return new TheoryData<string>
                {
                    "[item]",
                    "[(item,",
                    "(click)",
                    "(^click)",
                    "*something",
                    "#local",
                };
            }
        }

        [Theory]
        [MemberData(nameof(SymbolBoundAttributeNames))]
        public void SymbolBoundAttributes_BeforeEqualWhitespace(string attributeName)
        {
            // Arrange
            var attributeNameLength = attributeName.Length;
            var newlineLength = Environment.NewLine.Length;
            var prefixLocation1 = new SourceLocation(
                absoluteIndex: 2,
                lineIndex: 0,
                characterIndex: 2);
            var suffixLocation1 = new SourceLocation(
                absoluteIndex: 8 + newlineLength + attributeNameLength,
                lineIndex: 1,
                characterIndex: 5 + attributeNameLength);
            var valueLocation1 = new SourceLocation(
                absoluteIndex: 5 + attributeNameLength + newlineLength,
                lineIndex: 1,
                characterIndex: 2 + attributeNameLength);
            var prefixLocation2 = SourceLocation.Advance(suffixLocation1, "'");
            var suffixLocation2 = new SourceLocation(
                absoluteIndex: 15 + attributeNameLength * 2 + newlineLength * 2,
                lineIndex: 2,
                characterIndex: 4);
            var valueLocation2 = new SourceLocation(
                absoluteIndex: 12 + attributeNameLength * 2 + newlineLength * 2,
                lineIndex: 2,
                characterIndex: 1);

            // Act & Assert
            ParseBlockTest(
                $"<a {attributeName}{Environment.NewLine}='Foo'\t{attributeName}={Environment.NewLine}'Bar' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                attributeName,
                                prefix: new LocationTagged<string>(
                                    $" {attributeName}{Environment.NewLine}='", prefixLocation1),
                                suffix: new LocationTagged<string>("'", suffixLocation1)),
                            Factory.Markup($" {attributeName}{Environment.NewLine}='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, valueLocation1),
                                    value: new LocationTagged<string>("Foo", valueLocation1))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                attributeName,
                                prefix: new LocationTagged<string>(
                                    $"\t{attributeName}={Environment.NewLine}'", prefixLocation2),
                                suffix: new LocationTagged<string>("'", suffixLocation2)),
                            Factory.Markup($"\t{attributeName}={Environment.NewLine}'").With(SpanChunkGenerator.Null),
                            Factory.Markup("Bar").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, valueLocation2),
                                    value: new LocationTagged<string>("Bar", valueLocation2))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Theory]
        [MemberData(nameof(SymbolBoundAttributeNames))]
        public void SymbolBoundAttributes_Whitespace(string attributeName)
        {
            // Arrange
            var attributeNameLength = attributeName.Length;
            var newlineLength = Environment.NewLine.Length;
            var prefixLocation1 = new SourceLocation(
                absoluteIndex: 2,
                lineIndex: 0,
                characterIndex: 2);
            var suffixLocation1 = new SourceLocation(
                absoluteIndex: 10 + newlineLength + attributeNameLength,
                lineIndex: 1,
                characterIndex: 5 + attributeNameLength + newlineLength);
            var valueLocation1 = new SourceLocation(
                absoluteIndex: 7 + attributeNameLength + newlineLength,
                lineIndex: 1,
                characterIndex: 4 + attributeNameLength);
            var prefixLocation2 = SourceLocation.Advance(suffixLocation1, "'");
            var suffixLocation2 = new SourceLocation(
                absoluteIndex: 17 + attributeNameLength * 2 + newlineLength * 2,
                lineIndex: 2,
                characterIndex: 5 + attributeNameLength);
            var valueLocation2 = new SourceLocation(
                absoluteIndex: 14 + attributeNameLength * 2 + newlineLength * 2,
                lineIndex: 2,
                characterIndex: 2 + attributeNameLength);

            // Act & Assert
            ParseBlockTest(
                $"<a {Environment.NewLine}  {attributeName}='Foo'\t{Environment.NewLine}{attributeName}='Bar' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                attributeName,
                                prefix: new LocationTagged<string>(
                                    $" {Environment.NewLine}  {attributeName}='", prefixLocation1),
                                suffix: new LocationTagged<string>("'", suffixLocation1)),
                            Factory.Markup($" {Environment.NewLine}  {attributeName}='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, valueLocation1),
                                    value: new LocationTagged<string>("Foo", valueLocation1))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                attributeName,
                                prefix: new LocationTagged<string>(
                                    $"\t{Environment.NewLine}{attributeName}='", prefixLocation2),
                                suffix: new LocationTagged<string>("'", suffixLocation2)),
                            Factory.Markup($"\t{Environment.NewLine}{attributeName}='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Bar").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, valueLocation2),
                                    value: new LocationTagged<string>("Bar", valueLocation2))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Theory]
        [MemberData(nameof(SymbolBoundAttributeNames))]
        public void SymbolBoundAttributes(string attributeName)
        {
            // Arrange
            var attributeNameLength = attributeName.Length;
            var suffixLocation = 8 + attributeNameLength;
            var valueLocation = 5 + attributeNameLength;

            // Act & Assert
            ParseBlockTest($"<a {attributeName}='Foo' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                attributeName,
                                prefix: new LocationTagged<string>($" {attributeName}='", 2, 0, 2),
                                suffix: new LocationTagged<string>("'", suffixLocation, 0, suffixLocation)),
                            Factory.Markup($" {attributeName}='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, valueLocation, 0, valueLocation),
                                    value: new LocationTagged<string>("Foo", valueLocation, 0, valueLocation))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void SimpleLiteralAttribute()
        {
            ParseBlockTest("<a href='Foo' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                name: "href", prefix: new LocationTagged<string>(" href='", 2, 0, 2), suffix: new LocationTagged<string>("'", 12, 0, 12)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, 9, 0, 9), value: new LocationTagged<string>("Foo", 9, 0, 9))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void SimpleLiteralAttributeWithWhitespaceSurroundingEquals()
        {
            ParseBlockTest("<a href \f\r\n= \t\n'Foo' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                name: "href",
                                prefix: new LocationTagged<string>(" href \f\r\n= \t\n'", 2, 0, 2),
                                suffix: new LocationTagged<string>("'", 19, 2, 4)),
                            Factory.Markup(" href \f\r\n= \t\n'").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, 16, 2, 1), value: new LocationTagged<string>("Foo", 16, 2, 1))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void DynamicAttributeWithWhitespaceSurroundingEquals()
        {
            ParseBlockTest("<a href \n= \r\n'@Foo' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                name: "href",
                                prefix: new LocationTagged<string>(" href \n= \r\n'", 2, 0, 2),
                                suffix: new LocationTagged<string>("'", 18, 2, 5)),
                        Factory.Markup(" href \n= \r\n'").With(SpanChunkGenerator.Null),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 14, 2, 1), 14, 2, 1),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("Foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void MultiPartLiteralAttribute()
        {
            ParseBlockTest("<a href='Foo Bar Baz' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "href", prefix: new LocationTagged<string>(" href='", 2, 0, 2), suffix: new LocationTagged<string>("'", 20, 0, 20)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(string.Empty, 9, 0, 9), value: new LocationTagged<string>("Foo", 9, 0, 9))),
                            Factory.Markup(" Bar").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(" ", 12, 0, 12), value: new LocationTagged<string>("Bar", 13, 0, 13))),
                            Factory.Markup(" Baz").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(" ", 16, 0, 16), value: new LocationTagged<string>("Baz", 17, 0, 17))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void DoubleQuotedLiteralAttribute()
        {
            ParseBlockTest("<a href=\"Foo Bar Baz\" />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "href", prefix: new LocationTagged<string>(" href=\"", 2, 0, 2), suffix: new LocationTagged<string>("\"", 20, 0, 20)),
                            Factory.Markup(" href=\"").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(string.Empty, 9, 0, 9), value: new LocationTagged<string>("Foo", 9, 0, 9))),
                            Factory.Markup(" Bar").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(" ", 12, 0, 12), value: new LocationTagged<string>("Bar", 13, 0, 13))),
                            Factory.Markup(" Baz").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(" ", 16, 0, 16), value: new LocationTagged<string>("Baz", 17, 0, 17))),
                            Factory.Markup("\"").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void NewLinePrecedingAttribute()
        {
            ParseBlockTest("<a\r\nhref='Foo' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                name: "href",
                                prefix: new LocationTagged<string>("\r\nhref='", 2, 0, 2),
                                suffix: new LocationTagged<string>("'", 13, 1, 9)),
                            Factory.Markup("\r\nhref='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, 10, 1, 6),
                                    value: new LocationTagged<string>("Foo", 10, 1, 6))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void NewLineBetweenAttributes()
        {
            ParseBlockTest("<a\nhref='Foo'\r\nabcd='Bar' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(
                            name: "href",
                            prefix: new LocationTagged<string>("\nhref='", 2, 0, 2),
                            suffix: new LocationTagged<string>("'", 12, 1, 9)),
                        Factory.Markup("\nhref='").With(SpanChunkGenerator.Null),
                        Factory.Markup("Foo").With(
                            new LiteralAttributeChunkGenerator(
                                prefix: new LocationTagged<string>(string.Empty, 9, 1, 6),
                                value: new LocationTagged<string>("Foo", 9, 1, 6))),
                        Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                name: "abcd",
                                prefix: new LocationTagged<string>("\r\nabcd='", 13, 1, 10),
                                suffix: new LocationTagged<string>("'", 24, 2, 9)),
                            Factory.Markup("\r\nabcd='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Bar").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, 21, 2, 6),
                                    value: new LocationTagged<string>("Bar", 21, 2, 6))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void WhitespaceAndNewLinePrecedingAttribute()
        {
            ParseBlockTest("<a \t\r\nhref='Foo' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator(
                                name: "href",
                                prefix: new LocationTagged<string>(" \t\r\nhref='", 2, 0, 2),
                                suffix: new LocationTagged<string>("'", 15, 1, 9)),
                            Factory.Markup(" \t\r\nhref='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(
                                new LiteralAttributeChunkGenerator(
                                    prefix: new LocationTagged<string>(string.Empty, 12, 1, 6),
                                    value: new LocationTagged<string>("Foo", 12, 1, 6))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void UnquotedLiteralAttribute()
        {
            ParseBlockTest("<a href=Foo Bar Baz />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "href", prefix: new LocationTagged<string>(" href=", 2, 0, 2), suffix: new LocationTagged<string>(string.Empty, 11, 0, 11)),
                            Factory.Markup(" href=").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(string.Empty, 8, 0, 8), value: new LocationTagged<string>("Foo", 8, 0, 8)))),
                        new MarkupBlock(Factory.Markup(" Bar")),
                        new MarkupBlock(Factory.Markup(" Baz")),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void SimpleExpressionAttribute()
        {
            ParseBlockTest("<a href='@foo' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "href", prefix: new LocationTagged<string>(" href='", 2, 0, 2), suffix: new LocationTagged<string>("'", 13, 0, 13)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 9, 0, 9), 9, 0, 9),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void MultiValueExpressionAttribute()
        {
            ParseBlockTest("<a href='@foo bar @baz' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "href", prefix: new LocationTagged<string>(" href='", 2, 0, 2), suffix: new LocationTagged<string>("'", 22, 0, 22)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 9, 0, 9), 9, 0, 9),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace))),
                            Factory.Markup(" bar").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(" ", 13, 0, 13), new LocationTagged<string>("bar", 14, 0, 14))),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(" ", 17, 0, 17), 18, 0, 18),
                                Factory.Markup(" ").With(SpanChunkGenerator.Null),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("baz")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void VirtualPathAttributesWorkWithConditionalAttributes()
        {
            ParseBlockTest("<a href='@foo ~/Foo/Bar' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "href", prefix: new LocationTagged<string>(" href='", 2, 0, 2), suffix: new LocationTagged<string>("'", 23, 0, 23)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 9, 0, 9), 9, 0, 9),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace))),
                            Factory.Markup(" ~/Foo/Bar")
                                   .With(new LiteralAttributeChunkGenerator(
                                       new LocationTagged<string>(" ", 13, 0, 13),
                                       new LocationTagged<string>("~/Foo/Bar", 14, 0, 14))),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void UnquotedAttributeWithCodeWithSpacesInBlock()
        {
            ParseBlockTest("<input value=@foo />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<input"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "value", prefix: new LocationTagged<string>(" value=", 6, 0, 6), suffix: new LocationTagged<string>(string.Empty, 17, 0, 17)),
                            Factory.Markup(" value=").With(SpanChunkGenerator.Null),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 13, 0, 13), 13, 0, 13),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace)))),
                        Factory.Markup(" />").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void UnquotedAttributeWithCodeWithSpacesInDocument()
        {
            ParseDocumentTest("<input value=@foo />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<input"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "value", prefix: new LocationTagged<string>(" value=", 6, 0, 6), suffix: new LocationTagged<string>(string.Empty, 17, 0, 17)),
                            Factory.Markup(" value=").With(SpanChunkGenerator.Null),
                            new MarkupBlock(new DynamicAttributeBlockChunkGenerator(new LocationTagged<string>(string.Empty, 13, 0, 13), 13, 0, 13),
                                new ExpressionBlock(
                                    Factory.CodeTransition(),
                                    Factory.Code("foo")
                                           .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                           .Accepts(AcceptedCharacters.NonWhiteSpace)))),
                        Factory.Markup(" />"))));
        }

        [Fact]
        public void ConditionalAttributeCollapserDoesNotRewriteEscapedTransitions()
        {
            // Act
            var results = ParseDocument("<span foo='@@' />");
            var rewritingContext = new RewritingContext(results.Document, new ErrorSink());
            new ConditionalAttributeCollapser(new HtmlMarkupParser().BuildSpan).Rewrite(rewritingContext);
            var rewritten = rewritingContext.SyntaxTree;

            // Assert
            Assert.Equal(0, results.ParserErrors.Count());
            EvaluateParseTree(rewritten,
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<span"),
                        new MarkupBlock(
                            new AttributeBlockChunkGenerator("foo", new LocationTagged<string>(" foo='", 5, 0, 5), new LocationTagged<string>("'", 13, 0, 13)),
                            Factory.Markup(" foo='").With(SpanChunkGenerator.Null),
                            new MarkupBlock(
                                Factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 11, 0, 11), new LocationTagged<string>("@", 11, 0, 11))).Accepts(AcceptedCharacters.None),
                                Factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharacters.None)),
                            Factory.Markup("'").With(SpanChunkGenerator.Null)),
                        Factory.Markup(" />"))));
        }

        [Fact]
        public void ConditionalAttributesDoNotCreateExtraDataForEntirelyLiteralAttribute()
        {
            // Arrange
            const string code =
 @"<div class=""sidebar"">
    <h1>Title</h1>
    <p>
        As the author, you can <a href=""/Photo/Edit/photoId"">edit</a>
        or <a href=""/Photo/Remove/photoId"">remove</a> this photo.
    </p>
    <dl>
        <dt class=""description"">Description</dt>
        <dd class=""description"">
            The uploader did not provide a description for this photo.
        </dd>
        <dt class=""uploaded-by"">Uploaded by</dt>
        <dd class=""uploaded-by""><a href=""/User/View/user.UserId"">user.DisplayName</a></dd>
        <dt class=""upload-date"">Upload date</dt>
        <dd class=""upload-date"">photo.UploadDate</dd>
        <dt class=""part-of-gallery"">Gallery</dt>
        <dd><a href=""/View/gallery.Id"" title=""View gallery.Name gallery"">gallery.Name</a></dd>
        <dt class=""tags"">Tags</dt>
        <dd class=""tags"">
            <ul class=""tags"">
                <li>This photo has no tags.</li>
            </ul>
            <a href=""/Photo/EditTags/photoId"">edit tags</a>
        </dd>
    </dl>

    <p>
        <a class=""download"" href=""/Photo/Full/photoId"" title=""Download: (photo.FileTitle + photo.FileExtension)"">Download full photo</a> ((photo.FileSize / 1024) KB)
    </p>
</div>
<div class=""main"">
    <img class=""large-photo"" alt=""photo.FileTitle"" src=""/Photo/Thumbnail"" />
    <h2>Nobody has commented on this photo</h2>
    <ol class=""comments"">
        <li>
            <h3 class=""comment-header"">
                <a href=""/User/View/comment.UserId"" title=""View comment.DisplayName's profile"">comment.DisplayName</a> commented at comment.CommentDate:
            </h3>
            <p class=""comment-body"">comment.CommentText</p>
        </li>
    </ol>

    <form method=""post"" action="""">
        <fieldset id=""addComment"">
            <legend>Post new comment</legend>
            <ol>
                <li>
                    <label for=""newComment"">Comment</label>
                    <textarea id=""newComment"" name=""newComment"" title=""Your comment"" rows=""6"" cols=""70""></textarea>
                </li>
            </ol>
            <p class=""form-actions"">
                <input type=""submit"" title=""Add comment"" value=""Add comment"" />
            </p>
        </fieldset>
    </form>
</div>";

            // Act
            var results = ParseDocument(code);
            var rewritingContext = new RewritingContext(results.Document, new ErrorSink());
            new ConditionalAttributeCollapser(new HtmlMarkupParser().BuildSpan).Rewrite(rewritingContext);
            new MarkupCollapser(new HtmlMarkupParser().BuildSpan).Rewrite(rewritingContext);
            var rewritten = rewritingContext.SyntaxTree;

            // Assert
            Assert.Equal(0, results.ParserErrors.Count());
            Assert.Equal(rewritten.Children.Count(), results.Document.Children.Count());
        }

        [Fact]
        public void ConditionalAttributesAreDisabledForDataAttributesInBlock()
        {
            ParseBlockTest("<span data-foo='@foo'></span>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<span"),
                        new MarkupBlock(
                            Factory.Markup(" data-foo='"),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("foo")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("'")),
                        Factory.Markup(">").Accepts(AcceptedCharacters.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</span>").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void ConditionalAttributesWithWeirdSpacingAreDisabledForDataAttributesInBlock()
        {
            ParseBlockTest("<span data-foo  =  '@foo'></span>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<span"),
                        new MarkupBlock(
                            Factory.Markup(" data-foo  =  '"),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("foo")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("'")),
                        Factory.Markup(">").Accepts(AcceptedCharacters.None)),
                    new MarkupTagBlock(
                        Factory.Markup("</span>").Accepts(AcceptedCharacters.None))));
        }

        [Fact]
        public void ConditionalAttributesAreDisabledForDataAttributesInDocument()
        {
            ParseDocumentTest("<span data-foo='@foo'></span>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<span"),
                        new MarkupBlock(
                            Factory.Markup(" data-foo='"),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("foo")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace)),
                            Factory.Markup("'")),
                        Factory.Markup(">")),
                    new MarkupTagBlock(
                        Factory.Markup("</span>"))));
        }

        [Fact]
        public void ConditionalAttributesWithWeirdSpacingAreDisabledForDataAttributesInDocument()
        {
            ParseDocumentTest("<span data-foo=@foo ></span>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<span"),
                        new MarkupBlock(
                            Factory.Markup(" data-foo="),
                            new ExpressionBlock(
                                Factory.CodeTransition(),
                                Factory.Code("foo")
                                       .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                       .Accepts(AcceptedCharacters.NonWhiteSpace))),
                        Factory.Markup(" >")),
                    new MarkupTagBlock(
                        Factory.Markup("</span>"))));
        }
    }
}
