using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test
{
    public class TagHelperParseTreeRewriterTests
    {
        public void IsComment_ReturnsTrueForSpanInHtmlCommentBlock()
        {
            // Arrange
            SpanFactory spanFactory = new SpanFactory();

            Span content = spanFactory.Markup("<!-- comment -->");
            Block commentBlock = new HtmlCommentBlock(content);

            // Act
            bool actualResult = TagHelperParseTreeRewriter.IsComment(content);

            // Assert
            Assert.True(actualResult);
        }
    }
}