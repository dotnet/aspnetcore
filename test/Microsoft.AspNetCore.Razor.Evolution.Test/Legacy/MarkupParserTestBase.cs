// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal abstract class MarkupParserTestBase : CodeParserTestBase
    {
        protected override RazorSyntaxTree ParseBlock(string document, bool designTime)
        {
            return ParseHtmlBlock(document, designTime);
        }

        protected virtual void SingleSpanDocumentTest(string document, BlockType blockType, SpanKind spanType)
        {
            var b = CreateSimpleBlockAndSpan(document, blockType, spanType);
            ParseDocumentTest(document, b);
        }
    }
}
