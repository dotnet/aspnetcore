// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal sealed class DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode : ExtensionIntermediateNode
    {
        public override RazorDiagnosticCollection Diagnostics { get; } = ReadOnlyDiagnosticCollection.Instance;
        
        public override IntermediateNodeCollection Children => ReadOnlyIntermediateNodeCollection.Instance;

        public override bool HasDiagnostics => false;

        public string VariableName { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public AttributeStructure AttributeStructure { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode>(this, visitor);
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

            extension.WriteDeclarePreallocatedTagHelperHtmlAttribute(context, this);
        }
    }
}
