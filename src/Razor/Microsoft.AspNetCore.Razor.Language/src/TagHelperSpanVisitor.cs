// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

internal class TagHelperSpanVisitor : SyntaxWalker
{
    private readonly RazorSourceDocument _source;
    private readonly List<TagHelperSpanInternal> _spans;

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
