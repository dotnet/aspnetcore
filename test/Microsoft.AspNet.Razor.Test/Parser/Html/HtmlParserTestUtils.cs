// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;

namespace Microsoft.AspNet.Razor.Test.Parser.Html
{
    internal class HtmlParserTestUtils
    {
        public static void RunSingleAtEscapeTest(Action<string, Block> testMethod, AcceptedCharacters lastSpanAcceptedCharacters = AcceptedCharacters.None)
        {
            var factory = SpanFactory.CreateCsHtml();
            testMethod("<foo>@@bar</foo>",
                new MarkupBlock(
                    factory.Markup("<foo>"),
                    factory.Markup("@").Hidden(),
                    factory.Markup("@bar</foo>").Accepts(lastSpanAcceptedCharacters)));
        }

        public static void RunMultiAtEscapeTest(Action<string, Block> testMethod, AcceptedCharacters lastSpanAcceptedCharacters = AcceptedCharacters.None)
        {
            var factory = SpanFactory.CreateCsHtml();
            testMethod("<foo>@@@@@bar</foo>",
                new MarkupBlock(
                    factory.Markup("<foo>"),
                    factory.Markup("@").Hidden(),
                    factory.Markup("@"),
                    factory.Markup("@").Hidden(),
                    factory.Markup("@"),
                    new ExpressionBlock(
                        factory.CodeTransition(),
                        factory.Code("bar")
                               .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                               .Accepts(AcceptedCharacters.NonWhiteSpace)),
                    factory.Markup("</foo>").Accepts(lastSpanAcceptedCharacters)));
        }
    }
}
