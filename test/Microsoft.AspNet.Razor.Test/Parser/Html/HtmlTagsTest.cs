// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Parser.Html
{
    public class HtmlTagsTest : CsHtmlMarkupParserTestBase
    {
        public static IEnumerable<string[]> VoidElementNames
        {
            get
            {
                yield return new[] { "area" };
                yield return new[] { "base" };
                yield return new[] { "br" };
                yield return new[] { "col" };
                yield return new[] { "command" };
                yield return new[] { "embed" };
                yield return new[] { "hr" };
                yield return new[] { "img" };
                yield return new[] { "input" };
                yield return new[] { "keygen" };
                yield return new[] { "link" };
                yield return new[] { "meta" };
                yield return new[] { "param" };
                yield return new[] { "source" };
                yield return new[] { "track" };
                yield return new[] { "wbr" };
            }
        }

        [Fact]
        public void EmptyTagNestsLikeNormalTag()
        {
            ParseBlockTest("<p></> Bar",
                new MarkupBlock(
                    Factory.Markup("<p></> ").Accepts(AcceptedCharacters.None)),
                new RazorError(RazorResources.ParseError_MissingEndTag("p"), 0, 0, 0));
        }

        [Fact]
        public void EmptyTag()
        {
            ParseBlockTest("<></> Bar",
                new MarkupBlock(
                    Factory.Markup("<></> ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void CommentTag()
        {
            ParseBlockTest("<!--Foo--> Bar",
                new MarkupBlock(
                    Factory.Markup("<!--Foo--> ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void DocTypeTag()
        {
            ParseBlockTest("<!DOCTYPE html> foo",
                new MarkupBlock(
                    Factory.Markup("<!DOCTYPE html> ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ProcessingInstructionTag()
        {
            ParseBlockTest("<?xml version=\"1.0\" ?> foo",
                new MarkupBlock(
                    Factory.Markup("<?xml version=\"1.0\" ?> ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ElementTags()
        {
            ParseBlockTest("<p>Foo</p> Bar",
                new MarkupBlock(
                    Factory.Markup("<p>Foo</p> ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void TextTags()
        {
            ParseBlockTest("<text>Foo</text>}",
                new MarkupBlock(
                    Factory.MarkupTransition("<text>"),
                    Factory.Markup("Foo"),
                    Factory.MarkupTransition("</text>")));
        }

        [Fact]
        public void CDataTag()
        {
            ParseBlockTest("<![CDATA[Foo]]> Bar",
                new MarkupBlock(
                    Factory.Markup("<![CDATA[Foo]]> ").Accepts(AcceptedCharacters.None)));
        }

        [Fact]
        public void ScriptTag()
        {
            ParseDocumentTest("<script>foo < bar && quantity.toString() !== orderQty.val()</script>",
                new MarkupBlock(
                    Factory.Markup("<script>foo < bar && quantity.toString() !== orderQty.val()</script>")));
        }

        [Theory]
        [PropertyData("VoidElementNames")]
        public void VoidElementFollowedByContent(string tagName)
        {
            ParseBlockTest("<" + tagName + ">foo",
                new MarkupBlock(
                    Factory.Markup("<" + tagName + ">")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Theory]
        [PropertyData("VoidElementNames")]
        public void VoidElementFollowedByOtherTag(string tagName)
        {
            ParseBlockTest("<" + tagName + "><other>foo",
                new MarkupBlock(
                    Factory.Markup("<" + tagName + ">")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Theory]
        [PropertyData("VoidElementNames")]
        public void VoidElementFollowedByCloseTag(string tagName)
        {
            ParseBlockTest("<" + tagName + "> </" + tagName + ">foo",
                new MarkupBlock(
                    Factory.Markup("<" + tagName + "> </" + tagName + ">")
                           .Accepts(AcceptedCharacters.None)));
        }

        [Theory]
        [PropertyData("VoidElementNames")]
        public void IncompleteVoidElementEndTag(string tagName)
        {
            ParseBlockTest("<" + tagName + "></" + tagName,
                new MarkupBlock(
                    Factory.Markup("<" + tagName + "></" + tagName)
                           .Accepts(AcceptedCharacters.Any)));
        }
    }
}
