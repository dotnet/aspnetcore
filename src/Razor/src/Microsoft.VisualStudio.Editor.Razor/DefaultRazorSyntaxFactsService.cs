// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(RazorSyntaxFactsService))]
    internal class DefaultRazorSyntaxFactsService : RazorSyntaxFactsService
    {
        public override IReadOnlyList<ClassifiedSpan> GetClassifiedSpans(RazorSyntaxTree syntaxTree)
        {
            var result = syntaxTree.GetClassifiedSpans();
            return result.Select(item =>
            {
                return new ClassifiedSpan(
                    item.Span,
                    item.BlockSpan,
                    (SpanKind)item.SpanKind,
                    (BlockKind)item.BlockKind,
                    (AcceptedCharacters)item.AcceptedCharacters);
            }).ToArray();
        }

        public override IReadOnlyList<TagHelperSpan> GetTagHelperSpans(RazorSyntaxTree syntaxTree)
        {
            var result = syntaxTree.GetTagHelperSpans();
            return result.Select(item => new TagHelperSpan(item.Span, item.Binding)).ToArray();
        }
    }
}
