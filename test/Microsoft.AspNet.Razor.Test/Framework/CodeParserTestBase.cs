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

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Test.Framework
{
    public abstract class CodeParserTestBase : ParserTestBase
    {
        protected abstract ISet<string> KeywordSet { get; }

        protected override ParserBase SelectActiveParser(ParserBase codeParser, ParserBase markupParser)
        {
            return codeParser;
        }

        protected void ImplicitExpressionTest(string input, params RazorError[] errors)
        {
            ImplicitExpressionTest(input, AcceptedCharacters.NonWhiteSpace, errors);
        }

        protected void ImplicitExpressionTest(string input, AcceptedCharacters acceptedCharacters, params RazorError[] errors)
        {
            ImplicitExpressionTest(input, input, acceptedCharacters, errors);
        }

        protected void ImplicitExpressionTest(string input, string expected, params RazorError[] errors)
        {
            ImplicitExpressionTest(input, expected, AcceptedCharacters.NonWhiteSpace, errors);
        }

        protected override void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            SingleSpanBlockTest(document, blockType, spanType, acceptedCharacters, expectedError: null);
        }

        protected override void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            SingleSpanBlockTest(document, spanContent, blockType, spanType, acceptedCharacters, expectedErrors: null);
        }

        protected override void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockType, spanType, expectedError);
        }

        protected override void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, params RazorError[] expectedErrors)
        {
            SingleSpanBlockTest(document, spanContent, blockType, spanType, AcceptedCharacters.Any, expectedErrors ?? new RazorError[0]);
        }

        protected override void SingleSpanBlockTest(string document, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockType, spanType, acceptedCharacters, expectedError);
        }

        protected override void SingleSpanBlockTest(string document, string spanContent, BlockType blockType, SpanKind spanType, AcceptedCharacters acceptedCharacters, params RazorError[] expectedErrors)
        {
            Block b = CreateSimpleBlockAndSpan(spanContent, blockType, spanType, acceptedCharacters);
            ParseBlockTest(document, b, expectedErrors ?? new RazorError[0]);
        }

        protected void ImplicitExpressionTest(string input, string expected, AcceptedCharacters acceptedCharacters, params RazorError[] errors)
        {
            var factory = CreateSpanFactory();
            ParseBlockTest(SyntaxConstants.TransitionString + input,
                           new ExpressionBlock(
                               factory.CodeTransition(),
                               factory.Code(expected)
                                   .AsImplicitExpression(KeywordSet)
                                   .Accepts(acceptedCharacters)),
                           errors);
        }
    }
}
