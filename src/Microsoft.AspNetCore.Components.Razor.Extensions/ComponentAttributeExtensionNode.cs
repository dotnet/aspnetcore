// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class ComponentAttributeExtensionNode : ExtensionIntermediateNode
    {
        public ComponentAttributeExtensionNode()
        {
        }

        public ComponentAttributeExtensionNode(TagHelperHtmlAttributeIntermediateNode attributeNode)
        {
            if (attributeNode == null)
            {
                throw new ArgumentNullException(nameof(attributeNode));
            }

            AttributeName = attributeNode.AttributeName;
            AttributeStructure = attributeNode.AttributeStructure;
            Source = attributeNode.Source;

            for (var i = 0; i < attributeNode.Children.Count; i++)
            {
                Children.Add(attributeNode.Children[i]);
            }

            for (var i = 0; i < attributeNode.Diagnostics.Count; i++)
            {
                Diagnostics.Add(attributeNode.Diagnostics[i]);
            }
        }

        public ComponentAttributeExtensionNode(TagHelperPropertyIntermediateNode propertyNode)
        {
            if (propertyNode == null)
            {
                throw new ArgumentNullException(nameof(propertyNode));
            }

            AttributeName = propertyNode.AttributeName;
            AttributeStructure = propertyNode.AttributeStructure;
            BoundAttribute = propertyNode.BoundAttribute;
            PropertyName = propertyNode.BoundAttribute.GetPropertyName();
            Source = propertyNode.Source;
            TagHelper = propertyNode.TagHelper;
            TypeName = propertyNode.BoundAttribute.IsWeaklyTyped() ? null : propertyNode.BoundAttribute.TypeName;

            for (var i = 0; i < propertyNode.Children.Count; i++)
            {
                Children.Add(propertyNode.Children[i]);
            }

            for (var i = 0; i < propertyNode.Diagnostics.Count; i++)
            {
                Diagnostics.Add(propertyNode.Diagnostics[i]);
            }
        }

        public ComponentAttributeExtensionNode(ComponentAttributeExtensionNode attributeNode)
        {
            if (attributeNode == null)
            {
                throw new ArgumentNullException(nameof(attributeNode));
            }

            AttributeName = attributeNode.AttributeName;
            AttributeStructure = attributeNode.AttributeStructure;
            BoundAttribute = attributeNode.BoundAttribute;
            PropertyName = attributeNode.BoundAttribute.GetPropertyName();
            Source = attributeNode.Source;
            TagHelper = attributeNode.TagHelper;
            TypeName = attributeNode.BoundAttribute.IsWeaklyTyped() ? null : attributeNode.BoundAttribute.TypeName;

            for (var i = 0; i < attributeNode.Children.Count; i++)
            {
                Children.Add(attributeNode.Children[i]);
            }

            for (var i = 0; i < attributeNode.Diagnostics.Count; i++)
            {
                Diagnostics.Add(attributeNode.Diagnostics[i]);
            }
        }

        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public string AttributeName { get; set; }

        public AttributeStructure AttributeStructure { get; set; }

        public BoundAttributeDescriptor BoundAttribute { get; set; }

        public string PropertyName { get; set; }

        public TagHelperDescriptor TagHelper { get; set; }

        public string TypeName { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<ComponentAttributeExtensionNode>(this, visitor);
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
            writer.WriteComponentAttribute(context, this);
        }
    }
}
