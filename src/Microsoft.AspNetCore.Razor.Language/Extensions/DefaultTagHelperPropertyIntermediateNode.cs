// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class DefaultTagHelperPropertyIntermediateNode : ExtensionIntermediateNode
    {
        public DefaultTagHelperPropertyIntermediateNode()
        {
        }

        public DefaultTagHelperPropertyIntermediateNode(TagHelperPropertyIntermediateNode propertyNode)
        {
            if (propertyNode == null)
            {
                throw new ArgumentNullException(nameof(propertyNode));
            }

            AttributeName = propertyNode.AttributeName;
            AttributeStructure = propertyNode.AttributeStructure;
            BoundAttribute = propertyNode.BoundAttribute;
            IsIndexerNameMatch = propertyNode.IsIndexerNameMatch;
            Source = propertyNode.Source;
            TagHelper = propertyNode.TagHelper;

            for (var i = 0; i < propertyNode.Children.Count; i++)
            {
                Children.Add(propertyNode.Children[i]);
            }

            for (var i = 0; i < propertyNode.Diagnostics.Count; i++)
            {
                Diagnostics.Add(propertyNode.Diagnostics[i]);
            }
        }

        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public string AttributeName { get; set; }

        public AttributeStructure AttributeStructure { get; set; }

        public BoundAttributeDescriptor BoundAttribute { get; set; }

        public string Field { get; set; }

        public bool IsIndexerNameMatch { get; set; }

        public string Property { get; set; }

        public TagHelperDescriptor TagHelper { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<DefaultTagHelperPropertyIntermediateNode>(this, visitor);
        }

        public override void WriteNode(CodeTarget target, CodeRenderingContext context)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var extension = target.GetExtension<IDefaultTagHelperTargetExtension>();
            if (extension == null)
            {
                ReportMissingCodeTargetExtension<IDefaultTagHelperTargetExtension>(context);
                return;
            }

            extension.WriteTagHelperProperty(context, this);
        }
    }
}
