// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class MalformedDirectiveIRNode : RazorIRNode
    {
        private RazorDiagnosticCollection _diagnostics;

        public override ItemCollection Annotations { get; }

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

        public override RazorIRNodeCollection Children { get; } = new DefaultIRNodeCollection();

        public override SourceSpan? Source { get; set; }

        public override bool HasDiagnostics => _diagnostics != null && _diagnostics.Count > 0;

        public string Name { get; set; }

        public IEnumerable<DirectiveTokenIRNode> Tokens => Children.OfType<DirectiveTokenIRNode>();

        public DirectiveDescriptor Descriptor { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            visitor.VisitMalformedDirective(this);
        }
    }
}