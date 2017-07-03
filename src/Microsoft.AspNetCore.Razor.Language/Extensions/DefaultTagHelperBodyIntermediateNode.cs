// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class DefaultTagHelperBodyIntermediateNode : ExtensionIntermediateNode
    {
        public DefaultTagHelperBodyIntermediateNode()
        {
        }

        public DefaultTagHelperBodyIntermediateNode(TagHelperBodyIntermediateNode bodyNode)
        {
            if (bodyNode == null)
            {
                throw new ArgumentNullException(nameof(bodyNode));
            }

            Source = bodyNode.Source;

            for (var i = 0; i < bodyNode.Children.Count; i++)
            {
                Children.Add(bodyNode.Children[i]);
            }

            for (var i = 0; i < bodyNode.Diagnostics.Count; i++)
            {
                Diagnostics.Add(bodyNode.Diagnostics[i]);
            }
        }

        public override IntermediateNodeCollection Children { get; } = new DefaultIntermediateNodeCollection();

        public TagMode TagMode { get; set; }

        public string TagName { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<DefaultTagHelperBodyIntermediateNode>(this, visitor);
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

            extension.WriteTagHelperBody(context, this);
        }
    }
}
