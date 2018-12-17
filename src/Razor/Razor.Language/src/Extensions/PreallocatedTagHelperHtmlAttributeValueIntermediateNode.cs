// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal sealed class PreallocatedTagHelperHtmlAttributeValueIntermediateNode : ExtensionIntermediateNode
    {
        public PreallocatedTagHelperHtmlAttributeValueIntermediateNode()
        {
        }

        public PreallocatedTagHelperHtmlAttributeValueIntermediateNode(DefaultTagHelperHtmlAttributeIntermediateNode htmlAttributeNode)
        {
            if (htmlAttributeNode == null)
            {
                throw new ArgumentNullException(nameof(htmlAttributeNode));
            }

            Source = htmlAttributeNode.Source;

            for (var i = 0; i < htmlAttributeNode.Children.Count; i++)
            {
                Children.Add(htmlAttributeNode.Children[i]);
            }

            for (var i = 0; i < htmlAttributeNode.Diagnostics.Count; i++)
            {
                Diagnostics.Add(htmlAttributeNode.Diagnostics[i]);
            }
        }

        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public string VariableName { get; set; }

        public string AttributeName { get; set; }

        public string Value { get; set; }

        public AttributeStructure AttributeStructure { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<PreallocatedTagHelperHtmlAttributeValueIntermediateNode>(this, visitor);
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

            extension.WriteTagHelperHtmlAttributeValue(context, this);
        }

        public override void FormatNode(IntermediateNodeFormatter formatter)
        {
            formatter.WriteContent(AttributeName);

            formatter.WriteProperty(nameof(AttributeName), AttributeName);
            formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
            formatter.WriteProperty(nameof(Value), Value);
            formatter.WriteProperty(nameof(VariableName), VariableName);
        }
    }
}
