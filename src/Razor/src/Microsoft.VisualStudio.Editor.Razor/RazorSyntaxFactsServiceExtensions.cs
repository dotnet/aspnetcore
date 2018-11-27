// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public static class RazorSyntaxFactsServiceExtensions
    {
        public static bool IsTagHelperSpan(this RazorSyntaxFactsService syntaxFactsService, RazorSyntaxTree syntaxTree, SourceSpan span)
        {
            if (syntaxFactsService == null)
            {
                throw new ArgumentNullException(nameof(syntaxFactsService));
            }

            if (syntaxTree == null)
            {
                // Extra hardening for the case that tooling hasn't retrieved a SyntaxTree yet.
                return false;
            }

            var tagHelperSpans = syntaxFactsService.GetTagHelperSpans(syntaxTree);
            for (var i = 0; i < tagHelperSpans.Count; i++)
            {
                var tagHelperSpan = tagHelperSpans[i].Span;
                if (tagHelperSpan.AbsoluteIndex == span.AbsoluteIndex &&  tagHelperSpan.Length == span.Length)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
