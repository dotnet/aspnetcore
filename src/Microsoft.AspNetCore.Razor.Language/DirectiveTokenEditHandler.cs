// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DirectiveTokenEditHandler : SpanEditHandler
    {
        public DirectiveTokenEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer) : base(tokenizer)
        {
        }

        protected override PartialParseResult CanAcceptChange(Span target, SourceChange change)
        {
            if (AcceptedCharacters == AcceptedCharacters.NonWhiteSpace)
            {
                var originalText = change.GetOriginalText(target);
                var editedContent = change.GetEditedContent(target);

                if (!ContainsWhitespace(originalText) && !ContainsWhitespace(editedContent))
                {
                    // Did not modify whitespace, directive format should be the same.
                    // Return provisional so extensible IR/code gen pieces can see the full directive text
                    // once the user stops editing the document.
                    return PartialParseResult.Accepted | PartialParseResult.Provisional;
                }
            }

            return PartialParseResult.Rejected;

        }

        private static bool ContainsWhitespace(string content)
        {
            for (var i = 0; i < content.Length; i++)
            {
                if (char.IsWhiteSpace(content[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
