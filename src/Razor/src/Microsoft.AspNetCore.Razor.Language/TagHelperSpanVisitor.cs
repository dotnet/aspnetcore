// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class TagHelperSpanVisitor : SyntaxWalker
    {
        private RazorSourceDocument _source;
        private List<TagHelperSpanInternal> _spans;

        public TagHelperSpanVisitor(RazorSourceDocument source)
        {
            _source = source;
            _spans = new List<TagHelperSpanInternal>();
        }

        public IReadOnlyList<TagHelperSpanInternal> TagHelperSpans => _spans;

        public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
        {
            var span = new TagHelperSpanInternal(node.GetSourceSpan(_source), node.TagHelperInfo.BindingResult);
            _spans.Add(span);

            base.VisitMarkupTagHelperElement(node);
        }
    }
}
