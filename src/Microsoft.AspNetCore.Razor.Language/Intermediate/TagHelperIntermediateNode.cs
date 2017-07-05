// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class TagHelperIntermediateNode : IntermediateNode
    {
        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public TagMode TagMode { get; set; }

        public string TagName { get; set; }

        public ICollection<TagHelperDescriptor> TagHelpers { get; } = new List<TagHelperDescriptor>();

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
    }
}
