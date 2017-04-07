// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public abstract class MarkupParserTestBase : CodeParserTestBase
    {
        internal override RazorSyntaxTree ParseBlock(string document, bool designTime)
        {
            return ParseHtmlBlock(document, designTime);
        }

        internal virtual void SingleSpanDocumentTest(string document, BlockKind blockKind, SpanKind spanType)
        {
            var b = CreateSimpleBlockAndSpan(document, blockKind, spanType);
            ParseDocumentTest(document, b);
        }
    }
}
