// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal abstract class CodeParserTestBase : ParserTestBase
    {
        protected abstract ISet<string> KeywordSet { get; }

        protected override RazorSyntaxTree ParseBlock(string document, bool designTime)
        {
            return ParseCodeBlock(document, designTime);
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
            var b = CreateSimpleBlockAndSpan(spanContent, blockType, spanType, acceptedCharacters);
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
