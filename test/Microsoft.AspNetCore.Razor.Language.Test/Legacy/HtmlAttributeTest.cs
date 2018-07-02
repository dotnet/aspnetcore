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
        public void ConditionalAttributeCollapserDoesNotRewriteEscapedTransitions()
        {
            // Act
            var results = ParseDocument("<span foo='@@' />");
            var attributeCollapser = new ConditionalAttributeCollapser();
            var rewritten = attributeCollapser.Rewrite(results.Root);

            // Assert
            BaselineTest(rewritten);
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
            var attributeCollapser = new ConditionalAttributeCollapser();
            var rewritten = attributeCollapser.Rewrite(results.Root);

            // Assert
            Assert.Equal(rewritten.Children.Count(), results.Root.Children.Count());
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
