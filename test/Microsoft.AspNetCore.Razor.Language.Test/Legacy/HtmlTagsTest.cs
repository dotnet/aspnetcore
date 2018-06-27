// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class HtmlTagsTest : CsHtmlMarkupParserTestBase
    {
        private static readonly string[] VoidElementNames = new[]
        {
            "area",
            "base",
            "br",
            "col",
            "command",
            "embed",
            "hr",
            "img",
            "input",
            "keygen",
            "link",
            "meta",
            "param",
            "source",
            "track",
            "wbr",
        };

        public HtmlTagsTest()
        {
            UseBaselineTests = true;
        }

        [Fact]
        public void EmptyTagNestsLikeNormalTag()
        {
            ParseBlockTest("<p></> Bar");
        }

        [Fact]
        public void EmptyTag()
        {
            // This can happen in situations where a user is in VS' HTML editor and they're modifying
            // the contents of an HTML tag.
            ParseBlockTest("<></> Bar");
        }

        [Fact]
        public void CommentTag()
        {
            ParseBlockTest("<!--Foo--> Bar");
        }

        [Fact]
        public void DocTypeTag()
        {
            ParseBlockTest("<!DOCTYPE html> foo");
        }

        [Fact]
        public void ProcessingInstructionTag()
        {
            ParseBlockTest("<?xml version=\"1.0\" ?> foo");
        }

        [Fact]
        public void ElementTags()
        {
            ParseBlockTest("<p>Foo</p> Bar");
        }

        [Fact]
        public void TextTags()
        {
            ParseBlockTest("<text>Foo</text>}");
        }

        [Fact]
        public void CDataTag()
        {
            ParseBlockTest("<![CDATA[Foo]]> Bar");
        }

        [Fact]
        public void ScriptTag()
        {
            ParseDocumentTest("<script>foo < bar && quantity.toString() !== orderQty.val()</script>");
        }

        [Fact]
        public void ScriptTag_WithNestedMalformedTag()
        {
            ParseDocumentTest("<script>var four = 4; /* </ */</script>");
        }

        [Fact]
        public void ScriptTag_WithNestedEndTag()
        {
            ParseDocumentTest("<script></p></script>");
        }

        [Fact]
        public void ScriptTag_WithNestedBeginTag()
        {
            ParseDocumentTest("<script><p></script>");
        }

        [Fact]
        public void ScriptTag_WithNestedTag()
        {
            ParseDocumentTest("<script><p></p></script>");
        }

        [Fact]
        public void VoidElementFollowedByContent()
        {
            // Arrange
            var content = new StringBuilder();
            foreach (var tagName in VoidElementNames)
            {
                content.AppendLine("@{");
                content.AppendLine("<" + tagName + ">var x = true;");
                content.AppendLine("}");
            }

            // Act & Assert
            ParseDocumentTest(content.ToString());
        }

        [Fact]
        public void VoidElementFollowedByOtherTag()
        {
            // Arrange
            var content = new StringBuilder();
            foreach (var tagName in VoidElementNames)
            {
                content.AppendLine(@"{");
                content.AppendLine("<" + tagName + "><other> var x = true;");
                content.AppendLine("}");
            }

            // Act & Assert
            ParseDocumentTest(content.ToString());
        }

        [Fact]
        public void VoidElementFollowedByCloseTag()
        {
            // Arrange
            var content = new StringBuilder();
            foreach (var tagName in VoidElementNames)
            {
                content.AppendLine("@{");
                content.AppendLine("<" + tagName + "> </" + tagName + ">var x = true;");
                content.AppendLine("}");
            }

            // Act & Assert
            ParseDocumentTest(content.ToString());
        }

        [Fact]
        public void IncompleteVoidElementEndTag()
        {
            // Arrange
            var content = new StringBuilder();
            foreach (var tagName in VoidElementNames)
            {
                content.AppendLine("@{");
                content.AppendLine("<" + tagName + "></" + tagName);
                content.AppendLine("}");
            }

            // Act & Assert
            ParseDocumentTest(content.ToString());
        }
    }
}
