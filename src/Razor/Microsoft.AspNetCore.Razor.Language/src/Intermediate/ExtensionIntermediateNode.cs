// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class ExtensionIntermediateNode : IntermediateNode
    {
        public abstract void WriteNode(CodeTarget target, CodeRenderingContext context);

        protected static void AcceptExtensionNode<TNode>(TNode node, IntermediateNodeVisitor visitor)
            where TNode : ExtensionIntermediateNode
        {
            var typedVisitor = visitor as IExtensionIntermediateNodeVisitor<TNode>;
            if (typedVisitor == null)
            {
                visitor.VisitExtension(node);
            }
            else
            {
                typedVisitor.VisitExtension(node);
            }
        }

        protected void ReportMissingCodeTargetExtension<TDependency>(CodeRenderingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var documentKind = context.DocumentKind ?? string.Empty;
            context.Diagnostics.Add(
                RazorDiagnosticFactory.CreateCodeTarget_UnsupportedExtension(
                    documentKind, 
                    typeof(TDependency)));
        }
    }
}
