// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser.CSharp
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
                               Factory.Markup(" "),
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
