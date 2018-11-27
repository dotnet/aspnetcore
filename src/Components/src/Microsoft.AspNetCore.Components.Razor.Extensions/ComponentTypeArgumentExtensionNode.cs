// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class ComponentTypeArgumentExtensionNode : ExtensionIntermediateNode
    {
        public ComponentTypeArgumentExtensionNode(TagHelperPropertyIntermediateNode propertyNode)
        {
            if (propertyNode == null)
            {
                throw new ArgumentNullException(nameof(propertyNode));
            }

            BoundAttribute = propertyNode.BoundAttribute;
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

        public BoundAttributeDescriptor BoundAttribute { get; set; }

        public string TypeParameterName => BoundAttribute.Name;

        public TagHelperDescriptor TagHelper { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<ComponentTypeArgumentExtensionNode>(this, visitor);
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

            var writer = (BlazorNodeWriter)context.NodeWriter;
            writer.WriteComponentTypeArgument(context, this);
        }
    }
}
