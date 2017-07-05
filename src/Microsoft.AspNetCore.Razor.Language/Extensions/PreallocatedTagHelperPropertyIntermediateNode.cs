// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal sealed class PreallocatedTagHelperPropertyIntermediateNode : ExtensionIntermediateNode
    {
        public PreallocatedTagHelperPropertyIntermediateNode()
        {
        }

        public PreallocatedTagHelperPropertyIntermediateNode(DefaultTagHelperPropertyIntermediateNode propertyNode)
        {
            if (propertyNode == null)
            {
                throw new ArgumentNullException(nameof(propertyNode));
            }

            AttributeName = propertyNode.AttributeName;
            AttributeStructure = propertyNode.AttributeStructure;
            BoundAttribute = propertyNode.BoundAttribute;
            Field = propertyNode.Field;
            IsIndexerNameMatch = propertyNode.IsIndexerNameMatch;
            Property = propertyNode.Property;
            Source = propertyNode.Source;
            TagHelper = propertyNode.TagHelper;
        }

        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public string AttributeName { get; set; }

        public AttributeStructure AttributeStructure { get; set; }

        public BoundAttributeDescriptor BoundAttribute { get; set; }

        public string Field { get; set; }

        public bool IsIndexerNameMatch { get; set; }

        public string Property { get; set; }

        public TagHelperDescriptor TagHelper { get; set; }

        public string VariableName { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<PreallocatedTagHelperPropertyIntermediateNode>(this, visitor);
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

            var extension = target.GetExtension<IPreallocatedAttributeTargetExtension>();
            if (extension == null)
            {
                ReportMissingCodeTargetExtension<IPreallocatedAttributeTargetExtension>(context);
                return;
            }

            extension.WriteTagHelperProperty(context, this);
        }
    }
}
