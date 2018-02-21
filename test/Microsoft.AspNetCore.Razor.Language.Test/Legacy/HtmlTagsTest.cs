// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlTagsTest : CsHtmlMarkupParserTestBase
    {
        public static IEnumerable<object[]> VoidElementNames
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
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                    BlockFactory.MarkupTagBlock("</>", AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)),
                RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"));
        }

        [Fact]
        public void EmptyTag()
        {
            // This can happen in situations where a user is in VS' HTML editor and they're modifying
            // the contents of an HTML tag.
            ParseBlockTest("<></> Bar",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<>", AcceptedCharactersInternal.None),
                    BlockFactory.MarkupTagBlock("</>", AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void CommentTag()
        {
            ParseBlockTest("<!--Foo--> Bar",
                new MarkupBlock(
                    BlockFactory.HtmlCommentBlock("Foo"),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void DocTypeTag()
        {
            ParseBlockTest("<!DOCTYPE html> foo",
                new MarkupBlock(
                    Factory.Markup("<!DOCTYPE html>").Accepts(AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ProcessingInstructionTag()
        {
            ParseBlockTest("<?xml version=\"1.0\" ?> foo",
                new MarkupBlock(
                    Factory.Markup("<?xml version=\"1.0\" ?>").Accepts(AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ElementTags()
        {
            ParseBlockTest("<p>Foo</p> Bar",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<p>", AcceptedCharactersInternal.None),
                    Factory.Markup("Foo"),
                    BlockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void TextTags()
        {
            ParseBlockTest("<text>Foo</text>}",
                new MarkupBlock(
                    new MarkupTagBlock(
                        Factory.MarkupTransition("<text>")),
                    Factory.Markup("Foo").Accepts(AcceptedCharactersInternal.None),
                    new MarkupTagBlock(
                        Factory.MarkupTransition("</text>"))));
        }

        [Fact]
        public void CDataTag()
        {
            ParseBlockTest("<![CDATA[Foo]]> Bar",
                new MarkupBlock(
                    Factory.Markup("<![CDATA[Foo]]>").Accepts(AcceptedCharactersInternal.None),
                    Factory.Markup(" ").Accepts(AcceptedCharactersInternal.None)));
        }

        [Fact]
        public void ScriptTag()
        {
            ParseDocumentTest("<script>foo < bar && quantity.toString() !== orderQty.val()</script>",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<script>"),
                    Factory.Markup("foo < bar && quantity.toString() !== orderQty.val()"),
                    BlockFactory.MarkupTagBlock("</script>")));
        }

        [Fact]
        public void ScriptTag_WithNestedMalformedTag()
        {
            ParseDocumentTest("<script>var four = 4; /* </ */</script>",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<script>"),
                    Factory.Markup("var four = 4; /* </ */"),
                    BlockFactory.MarkupTagBlock("</script>")));
        }

        [Fact]
        public void ScriptTag_WithNestedEndTag()
        {
            ParseDocumentTest("<script></p></script>",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<script>"),
                    Factory.Markup("</p>"),
                    BlockFactory.MarkupTagBlock("</script>")));
        }

        [Fact]
        public void ScriptTag_WithNestedBeginTag()
        {
            ParseDocumentTest("<script><p></script>",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<script>"),
                    Factory.Markup("<p>"),
                    BlockFactory.MarkupTagBlock("</script>")));
        }

        [Fact]
        public void ScriptTag_WithNestedTag()
        {
            ParseDocumentTest("<script><p></p></script>",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<script>"),
                    Factory.Markup("<p></p>"),
                    BlockFactory.MarkupTagBlock("</script>")));
        }

        [Theory]
        [MemberData(nameof(VoidElementNames))]
        public void VoidElementFollowedByContent(string tagName)
        {
            ParseBlockTest("<" + tagName + ">foo",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<" + tagName + ">", AcceptedCharactersInternal.None)));
        }

        [Theory]
        [MemberData(nameof(VoidElementNames))]
        public void VoidElementFollowedByOtherTag(string tagName)
        {
            ParseBlockTest("<" + tagName + "><other>foo",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<" + tagName + ">", AcceptedCharactersInternal.None)));
        }

        [Theory]
        [MemberData(nameof(VoidElementNames))]
        public void VoidElementFollowedByCloseTag(string tagName)
        {
            ParseBlockTest("<" + tagName + "> </" + tagName + ">foo",
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<" + tagName + ">", AcceptedCharactersInternal.None),
                    Factory.Markup(" "),
                    BlockFactory.MarkupTagBlock("</" + tagName + ">", AcceptedCharactersInternal.None)));
        }

        [Theory]
        [MemberData(nameof(VoidElementNames))]
        public void IncompleteVoidElementEndTag(string tagName)
        {
            ParseBlockTest("<" + tagName + "></" + tagName,
                new MarkupBlock(
                    BlockFactory.MarkupTagBlock("<" + tagName + ">", AcceptedCharactersInternal.None),
                    BlockFactory.MarkupTagBlock("</" + tagName)));
        }
    }
}
