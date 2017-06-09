// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal sealed class SetPreallocatedTagHelperPropertyIRNode : ExtensionIRNode
    {
        public override RazorIRNodeCollection Children => ReadOnlyIRNodeCollection.Instance;

        public override SourceSpan? Source { get; set; }

        public string VariableName { get; set; }

        public string AttributeName { get; set; }

        public string TagHelperTypeName { get; set; }

        public string PropertyName { get; set; }

        public BoundAttributeDescriptor Descriptor { get; set; }

        public TagHelperBinding Binding { get; set; }

        public bool IsIndexerNameMatch { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<SetPreallocatedTagHelperPropertyIRNode>(this, visitor);
        }

        public override void WriteNode(CodeTarget target, CSharpRenderingContext context)
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
                context.ReportMissingExtension<IPreallocatedAttributeTargetExtension>();
                return;
            }

            extension.WriteSetPreallocatedTagHelperProperty(context, this);
        }
    }
}
