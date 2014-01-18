// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Test.Framework
{
    public abstract class MarkupParserTestBase : CodeParserTestBase
    {
        protected override ParserBase SelectActiveParser(ParserBase codeParser, ParserBase markupParser)
        {
            return markupParser;
        }

        protected virtual void SingleSpanDocumentTest(string document, BlockType blockType, SpanKind spanType)
        {
            Block b = CreateSimpleBlockAndSpan(document, blockType, spanType);
            ParseDocumentTest(document, b);
        }
    }
}
