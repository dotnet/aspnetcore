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
                new CSharpSymbol("\r\n", CSharpSymbolType.NewLine),
                new CSharpSymbol(" ", CSharpSymbolType.WhiteSpace),
                new CSharpSymbol("@", CSharpSymbolType.Transition),
                new CSharpSymbol("something", CSharpSymbolType.Identifier),
                new CSharpSymbol(" ", CSharpSymbolType.WhiteSpace),
                new CSharpSymbol("\r\n", CSharpSymbolType.NewLine));
        }

        [Fact]
        public void Next_IncludesComments_ReturnsNull_AfterTokenizingFirstDirective()
        {
            TestTokenizer(
                "@*included*@\r\n @something   \"value\"\r\n @this is ignored",
                new CSharpSymbol("@", CSharpSymbolType.RazorCommentTransition),
                new CSharpSymbol("*", CSharpSymbolType.RazorCommentStar),
                new CSharpSymbol("included", CSharpSymbolType.RazorComment),
                new CSharpSymbol("*", CSharpSymbolType.RazorCommentStar),
                new CSharpSymbol("@", CSharpSymbolType.RazorCommentTransition),
                new CSharpSymbol("\r\n", CSharpSymbolType.NewLine),
                new CSharpSymbol(" ", CSharpSymbolType.WhiteSpace),
                new CSharpSymbol("@", CSharpSymbolType.Transition),
                new CSharpSymbol("something", CSharpSymbolType.Identifier),
                new CSharpSymbol("   ", CSharpSymbolType.WhiteSpace),
                new CSharpSymbol("\"value\"", CSharpSymbolType.StringLiteral),
                new CSharpSymbol("\r\n", CSharpSymbolType.NewLine));
        }

        internal override object CreateTokenizer(ITextDocument source)
        {
            return new DirectiveCSharpTokenizer(source);
        }
    }
}
