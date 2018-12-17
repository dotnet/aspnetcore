// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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
                                   .Accepts(AcceptedCharactersInternal.None),
                               Factory.MetaMarkup(":", HtmlSymbolType.Colon),
                               Factory.Markup(" ")
                                   .With(new SpanEditHandler(
                                       CSharpLanguageCharacteristics.Instance.TokenizeString,
                                       AcceptedCharactersInternal.Any)),
                               new StatementBlock(
                                   Factory.CodeTransition()
                                       .Accepts(AcceptedCharactersInternal.None),
                                   Factory.Code("if (true) { }")
                                       .AsStatement()
                                   ),
                               Factory.Markup(Environment.NewLine)
                                   .Accepts(AcceptedCharactersInternal.None)));
        }
    }
}
