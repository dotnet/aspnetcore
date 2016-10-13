// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class HtmlParserTestUtils
    {
        public static void RunSingleAtEscapeTest(Action<string, Block> testMethod, AcceptedCharacters lastSpanAcceptedCharacters = AcceptedCharacters.None)
        {
            var factory = new SpanFactory();
            testMethod("<foo>@@bar</foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        factory.Markup("<foo>").Accepts(lastSpanAcceptedCharacters)),
                    factory.Markup("@").Hidden(),
                    factory.Markup("@bar"),
                    new MarkupTagBlock(
                        factory.Markup("</foo>").Accepts(lastSpanAcceptedCharacters))));
        }

        public static void RunMultiAtEscapeTest(Action<string, Block> testMethod, AcceptedCharacters lastSpanAcceptedCharacters = AcceptedCharacters.None)
        {
            var factory = new SpanFactory();
            testMethod("<foo>@@@@@bar</foo>",
                new MarkupBlock(
                    new MarkupTagBlock(
                        factory.Markup("<foo>").Accepts(lastSpanAcceptedCharacters)),
                    factory.Markup("@").Hidden(),
                    factory.Markup("@"),
                    factory.Markup("@").Hidden(),
                    factory.Markup("@"),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    new MarkupTagBlock(
                        factory.Markup("</foo>").Accepts(lastSpanAcceptedCharacters))));
        }
    }
}
