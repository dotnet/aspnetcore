// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public abstract class MarkupParserTestBase : CodeParserTestBase
    {
        internal override RazorSyntaxTree ParseBlock(
            RazorLanguageVersion version,
            string document,
            IEnumerable<DirectiveDescriptor> directives,
            bool designTime)
        {
            return ParseHtmlBlock(version, document, directives, designTime);
        }

        internal virtual void SingleSpanDocumentTest(string document, BlockKindInternal blockKind, SpanKindInternal spanType)
        {
            var b = CreateSimpleBlockAndSpan(document, blockKind, spanType);
            ParseDocumentTest(document, b);
        }
    }
}
