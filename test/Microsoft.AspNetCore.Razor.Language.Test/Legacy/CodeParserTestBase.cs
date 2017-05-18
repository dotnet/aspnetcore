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
            ImplicitExpressionTest(input, AcceptedCharactersInternal.NonWhiteSpace, errors);
        }

        internal void ImplicitExpressionTest(string input, AcceptedCharactersInternal acceptedCharacters, params RazorError[] errors)
        {
            ImplicitExpressionTest(input, input, acceptedCharacters, errors);
        }

        internal void ImplicitExpressionTest(string input, string expected, params RazorError[] errors)
        {
            ImplicitExpressionTest(input, expected, AcceptedCharactersInternal.NonWhiteSpace, errors);
        }

        internal override void SingleSpanBlockTest(string document, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            SingleSpanBlockTest(document, blockKind, spanType, acceptedCharacters, expectedError: null);
        }

        internal override void SingleSpanBlockTest(string document, string spanContent, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters = AcceptedCharactersInternal.Any)
        {
            SingleSpanBlockTest(document, spanContent, blockKind, spanType, acceptedCharacters, expectedErrors: null);
        }

        internal override void SingleSpanBlockTest(string document, BlockKindInternal blockKind, SpanKindInternal spanType, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockKind, spanType, expectedError);
        }

        internal override void SingleSpanBlockTest(string document, string spanContent, BlockKindInternal blockKind, SpanKindInternal spanType, params RazorError[] expectedErrors)
        {
            SingleSpanBlockTest(document, spanContent, blockKind, spanType, AcceptedCharactersInternal.Any, expectedErrors ?? new RazorError[0]);
        }

        internal override void SingleSpanBlockTest(string document, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters, params RazorError[] expectedError)
        {
            SingleSpanBlockTest(document, document, blockKind, spanType, acceptedCharacters, expectedError);
        }

        internal override void SingleSpanBlockTest(string document, string spanContent, BlockKindInternal blockKind, SpanKindInternal spanType, AcceptedCharactersInternal acceptedCharacters, params RazorError[] expectedErrors)
        {
            var b = CreateSimpleBlockAndSpan(spanContent, blockKind, spanType, acceptedCharacters);
            ParseBlockTest(document, b, expectedErrors ?? new RazorError[0]);
        }

        internal void ImplicitExpressionTest(string input, string expected, AcceptedCharactersInternal acceptedCharacters, params RazorError[] errors)
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
