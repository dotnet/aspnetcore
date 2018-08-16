// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DirectiveTokenEditHandler : SpanEditHandler
    {
        public DirectiveTokenEditHandler(Func<string, IEnumerable<SyntaxToken>> tokenizer) : base(tokenizer)
        {
        }

        protected override PartialParseResultInternal CanAcceptChange(Span target, SourceChange change)
        {
            if (AcceptedCharacters == AcceptedCharactersInternal.NonWhiteSpace)
            {
                var originalText = change.GetOriginalText(target);
                var editedContent = change.GetEditedContent(target);

                if (!ContainsWhitespace(originalText) && !ContainsWhitespace(editedContent))
                {
                    // Did not modify whitespace, directive format should be the same.
                    // Return provisional so extensible IR/code gen pieces can see the full directive text
                    // once the user stops editing the document.
                    return PartialParseResultInternal.Accepted | PartialParseResultInternal.Provisional;
                }
            }

            return PartialParseResultInternal.Rejected;

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
