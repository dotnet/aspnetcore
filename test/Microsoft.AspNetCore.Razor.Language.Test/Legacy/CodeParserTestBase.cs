// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public abstract class CodeParserTestBase : ParserTestBase
    {
        internal abstract ISet<string> KeywordSet { get; }

        internal override RazorSyntaxTree ParseBlock(string document, bool designTime)
        {
            return ParseCodeBlock(document, designTime);
        }

        internal void ImplicitExpressionTest(string input, params RazorError[] errors)
        {
            ImplicitExpressionTest(input, AcceptedCharacters.NonWhiteSpace, errors);
        }

        internal void ImplicitExpressionTest(string input, AcceptedCharacters acceptedCharacters, params RazorError[] errors)
        {
            ImplicitExpressionTest(input, input, acceptedCharacters, errors);
        }

        internal void ImplicitExpressionTest(string input, string expected, params RazorError[] errors)
        {
            ImplicitExpressionTest(input, expected, AcceptedCharacters.NonWhiteSpace, errors);
        }

        internal override void SingleSpanBlockTest(string document, BlockKind blockKind, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            SingleSpanBlockTest(document, blockKind, spanType, acceptedCharacters, expectedError: null);
        }

        internal override void SingleSpanBlockTest(string document, string spanContent, BlockKind blockKind, SpanKind spanType, AcceptedCharacters acceptedCharacters = AcceptedCharacters.Any)
        {
            SingleSpanBlockTest(document, spanContent, blockKind, spanType, acceptedCharacters, expectedErrors: null);
        }

        internal override void SingleSpanBlockTest(string document, BlockKind blockKind, SpanKind spanType, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockKind, spanType, expectedError);
        }

        internal override void SingleSpanBlockTest(string document, string spanContent, BlockKind blockKind, SpanKind spanType, params RazorError[] expectedErrors)
        {
            SingleSpanBlockTest(document, spanContent, blockKind, spanType, AcceptedCharacters.Any, expectedErrors ?? new RazorError[0]);
        }

        internal override void SingleSpanBlockTest(string document, BlockKind blockKind, SpanKind spanType, AcceptedCharacters acceptedCharacters, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockKind, spanType, acceptedCharacters, expectedError);
        }

        internal override void SingleSpanBlockTest(string document, string spanContent, BlockKind blockKind, SpanKind spanType, AcceptedCharacters acceptedCharacters, params RazorError[] expectedErrors)
        {
            var b = CreateSimpleBlockAndSpan(spanContent, blockKind, spanType, acceptedCharacters);
            ParseBlockTest(document, b, expectedErrors ?? new RazorError[0]);
        }

        internal void ImplicitExpressionTest(string input, string expected, AcceptedCharacters acceptedCharacters, params RazorError[] errors)
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
