// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
