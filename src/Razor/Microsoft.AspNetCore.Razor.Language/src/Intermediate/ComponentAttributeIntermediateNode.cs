// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class ComponentAttributeIntermediateNode : IntermediateNode
    {
        public ComponentAttributeIntermediateNode()
        {
        }

        public ComponentAttributeIntermediateNode(TagHelperHtmlAttributeIntermediateNode attributeNode)
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

        public ComponentAttributeIntermediateNode(TagHelperPropertyIntermediateNode propertyNode)
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

            visitor.VisitComponentAttribute(this);
        }

        public override void FormatNode(IntermediateNodeFormatter formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            
            formatter.WriteContent(AttributeName);

            formatter.WriteProperty(nameof(AttributeName), AttributeName);
            formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
            formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
            formatter.WriteProperty(nameof(PropertyName), PropertyName);
            formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
            formatter.WriteProperty(nameof(TypeName), TypeName);
        }
    }
}
