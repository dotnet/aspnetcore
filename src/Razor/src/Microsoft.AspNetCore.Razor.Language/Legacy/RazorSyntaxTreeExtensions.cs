// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal static class RazorSyntaxTreeExtensions
    {
        public static IReadOnlyList<ClassifiedSpanInternal> GetClassifiedSpans(this RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var rewriter = new ClassifiedSpanRewriter();
            var rewritten = rewriter.Visit(syntaxTree.Root);
            var visitor = new ClassifiedSpanVisitor(syntaxTree.Source);
            visitor.Visit(rewritten);

            return visitor.ClassifiedSpans;
        }

        public static IReadOnlyList<TagHelperSpanInternal> GetTagHelperSpans(this RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var visitor = new TagHelperSpanVisitor(syntaxTree.Source);
            visitor.Visit(syntaxTree.Root);

            return visitor.TagHelperSpans;
        }
    }
}
