// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.Html
{
    public class HtmlAttributeTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void SimpleLiteralAttribute()
        {
            ParseBlockTest("<a href='Foo' />",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.Markup("<a"),
                        new MarkupBlock(new AttributeBlockChunkGenerator(name: "href", prefix: new LocationTagged<string>(" href='", 2, 0, 2), suffix: new LocationTagged<string>("'", 12, 0, 12)),
                            Factory.Markup(" href='").With(SpanChunkGenerator.Null),
                            Factory.Markup("Foo").With(new LiteralAttributeChunkGenerator(prefix: new LocationTagged<string>(string.Empty, 9, 0, 9), value: new LocationTagged<string>("Foo", 9, 0, 9))),
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
    }
}
