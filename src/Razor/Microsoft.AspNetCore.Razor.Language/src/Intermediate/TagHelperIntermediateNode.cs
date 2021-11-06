// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public TagMode TagMode { get; set; }

    public string TagName { get; set; }

    public IList<TagHelperDescriptor> TagHelpers { get; } = new List<TagHelperDescriptor>();

    public TagHelperBodyIntermediateNode Body => Children.OfType<TagHelperBodyIntermediateNode>().SingleOrDefault();

    public IEnumerable<TagHelperPropertyIntermediateNode> Properties
    {
        get
        {
            return Children.OfType<TagHelperPropertyIntermediateNode>();
        }
    }

    public IEnumerable<TagHelperHtmlAttributeIntermediateNode> HtmlAttributes
    {
        get
        {
            return Children.OfType<TagHelperHtmlAttributeIntermediateNode>();
        }
    }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitTagHelper(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TagName);

        formatter.WriteProperty(nameof(TagHelpers), string.Join(", ", TagHelpers.Select(t => t.DisplayName)));
        formatter.WriteProperty(nameof(TagMode), TagMode.ToString());
        formatter.WriteProperty(nameof(TagName), TagName);
    }
}
