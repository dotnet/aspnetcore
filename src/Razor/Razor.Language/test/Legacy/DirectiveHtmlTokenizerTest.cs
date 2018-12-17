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
                new HtmlSymbol("\r\n", HtmlSymbolType.NewLine),
                new HtmlSymbol(" ", HtmlSymbolType.WhiteSpace),
                new HtmlSymbol("<", HtmlSymbolType.OpenAngle));
        }

        [Fact]
        public void Next_IncludesRazorComments_ReturnsNull_WhenHtmlIsSeen()
        {
            TestTokenizer(
                "\r\n @*included*@ <div>Ignored</div>",
                new HtmlSymbol("\r\n", HtmlSymbolType.NewLine),
                new HtmlSymbol(" ", HtmlSymbolType.WhiteSpace),
                new HtmlSymbol("@", HtmlSymbolType.RazorCommentTransition),
                new HtmlSymbol("*", HtmlSymbolType.RazorCommentStar),
                new HtmlSymbol("included", HtmlSymbolType.RazorComment),
                new HtmlSymbol("*", HtmlSymbolType.RazorCommentStar),
                new HtmlSymbol("@", HtmlSymbolType.RazorCommentTransition),
                new HtmlSymbol(" ", HtmlSymbolType.WhiteSpace),
                new HtmlSymbol("<", HtmlSymbolType.OpenAngle));
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new DirectiveHtmlTokenizer(source);
        }
    }
}
