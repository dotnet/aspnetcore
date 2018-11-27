// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class DirectiveCSharpTokenizerTest : CSharpTokenizerTestBase
    {
        [Fact]
        public void Next_ReturnsNull_AfterTokenizingFirstDirective()
        {
            TestTokenizer(
                "\r\n @something \r\n @this is ignored",
                new CSharpToken("\r\n", CSharpTokenType.NewLine),
                new CSharpToken(" ", CSharpTokenType.WhiteSpace),
                new CSharpToken("@", CSharpTokenType.Transition),
                new CSharpToken("something", CSharpTokenType.Identifier),
                new CSharpToken(" ", CSharpTokenType.WhiteSpace),
                new CSharpToken("\r\n", CSharpTokenType.NewLine));
        }

        [Fact]
        public void Next_IncludesComments_ReturnsNull_AfterTokenizingFirstDirective()
        {
            TestTokenizer(
                "@*included*@\r\n @something   \"value\"\r\n @this is ignored",
                new CSharpToken("@", CSharpTokenType.RazorCommentTransition),
                new CSharpToken("*", CSharpTokenType.RazorCommentStar),
                new CSharpToken("included", CSharpTokenType.RazorComment),
                new CSharpToken("*", CSharpTokenType.RazorCommentStar),
                new CSharpToken("@", CSharpTokenType.RazorCommentTransition),
                new CSharpToken("\r\n", CSharpTokenType.NewLine),
                new CSharpToken(" ", CSharpTokenType.WhiteSpace),
                new CSharpToken("@", CSharpTokenType.Transition),
                new CSharpToken("something", CSharpTokenType.Identifier),
                new CSharpToken("   ", CSharpTokenType.WhiteSpace),
                new CSharpToken("\"value\"", CSharpTokenType.StringLiteral),
                new CSharpToken("\r\n", CSharpTokenType.NewLine));
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new DirectiveCSharpTokenizer(source);
        }
    }
}
