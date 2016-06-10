// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Editor;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Test.Framework;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.Parser.CSharp
{
    public class CSharpWhitespaceHandlingTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void StatementBlockDoesNotAcceptTrailingNewlineIfNewlinesAreSignificantToAncestor()
        {
            ParseBlockTest("@: @if (true) { }" + Environment.NewLine
                         + "}",
                           new MarkupBlock(
                               Factory.MarkupTransition()
                                   .Accepts(AcceptedCharacters.None),
                               Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                               Factory.Markup(" ")
                                   .With(new SingleLineMarkupEditHandler(
                                       CSharpLanguageCharacteristics.Instance.TokenizeString,
                                       AcceptedCharacters.Any)),
                               new StatementBlock(
                                   Factory.CodeTransition()
                                       .Accepts(AcceptedCharacters.None),
                                   Factory.Code("if (true) { }")
                                       .AsStatement()
                                   ),
                               Factory.Markup(Environment.NewLine)
                                   .Accepts(AcceptedCharacters.None)));
        }
    }
}
