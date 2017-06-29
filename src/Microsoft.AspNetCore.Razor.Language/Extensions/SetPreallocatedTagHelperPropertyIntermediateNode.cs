// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal sealed class SetPreallocatedTagHelperPropertyIntermediateNode : ExtensionIntermediateNode
    {
        public override RazorDiagnosticCollection Diagnostics => ReadOnlyDiagnosticCollection.Instance;
        
        public override IntermediateNodeCollection Children => ReadOnlyIntermediateNodeCollection.Instance;

        public override bool HasDiagnostics => false;

        public string VariableName { get; set; }

        public string AttributeName { get; set; }

        public string TagHelperTypeName { get; set; }

        public string PropertyName { get; set; }

        public BoundAttributeDescriptor Descriptor { get; set; }

        public TagHelperBinding Binding { get; set; }

        public bool IsIndexerNameMatch { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<SetPreallocatedTagHelperPropertyIntermediateNode>(this, visitor);
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

            extension.WriteSetPreallocatedTagHelperProperty(context, this);
        }
    }
}
