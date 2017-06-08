// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class ExtensionIRNode : RazorIRNode
    {
        private ItemCollection _annotations;
        private RazorDiagnosticCollection _diagnostics;

        public override ItemCollection Annotations
        {
            get
            {
                if (_annotations == null)
                {
                    _annotations = new DefaultItemCollection();
                }

                return _annotations;
            }
        }

        public override RazorDiagnosticCollection Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new DefaultDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public override bool HasDiagnostics => _diagnostics != null && _diagnostics.Count > 0;

        public abstract void WriteNode(CodeTarget target, CSharpRenderingContext context);

        protected static void AcceptExtensionNode<TNode>(TNode node, RazorIRNodeVisitor visitor)
            where TNode : ExtensionIRNode
        {
            var typedVisitor = visitor as IExtensionIRNodeVisitor<TNode>;
            if (typedVisitor == null)
            {
                visitor.VisitExtension(node);
            }
            else
            {
                typedVisitor.VisitExtension(node);
            }
        }
    }
}
