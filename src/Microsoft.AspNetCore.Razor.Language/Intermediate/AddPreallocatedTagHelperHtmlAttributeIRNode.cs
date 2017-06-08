// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal sealed class AddPreallocatedTagHelperHtmlAttributeIRNode : ExtensionIRNode
    {
        public override RazorDiagnosticCollection Diagnostics { get; } = ReadOnlyDiagnosticCollection.Instance;
        
        public override RazorIRNodeCollection Children => ReadOnlyIRNodeCollection.Instance;

        public override SourceSpan? Source { get; set; }

        public override bool HasDiagnostics => false;

        public string VariableName { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<AddPreallocatedTagHelperHtmlAttributeIRNode>(this, visitor);
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

            extension.WriteAddPreallocatedTagHelperHtmlAttribute(context, this);
        }
    }
}
