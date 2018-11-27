// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class DirectiveHtmlTokenizerTest : HtmlTokenizerTestBase
    {
        [Fact]
        public void Next_ReturnsNull_WhenHtmlIsSeen()
        {
            TestTokenizer(
                "\r\n <div>Ignored</div>",
                new HtmlToken("\r\n", HtmlTokenType.NewLine),
                new HtmlToken(" ", HtmlTokenType.WhiteSpace),
                new HtmlToken("<", HtmlTokenType.OpenAngle));
        }

        [Fact]
        public void Next_IncludesRazorComments_ReturnsNull_WhenHtmlIsSeen()
        {
            TestTokenizer(
                "\r\n @*included*@ <div>Ignored</div>",
                new HtmlToken("\r\n", HtmlTokenType.NewLine),
                new HtmlToken(" ", HtmlTokenType.WhiteSpace),
                new HtmlToken("@", HtmlTokenType.RazorCommentTransition),
                new HtmlToken("*", HtmlTokenType.RazorCommentStar),
                new HtmlToken("included", HtmlTokenType.RazorComment),
                new HtmlToken("*", HtmlTokenType.RazorCommentStar),
                new HtmlToken("@", HtmlTokenType.RazorCommentTransition),
                new HtmlToken(" ", HtmlTokenType.WhiteSpace),
                new HtmlToken("<", HtmlTokenType.OpenAngle));
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new DirectiveHtmlTokenizer(source);
        }
    }
}
