// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class MalformedDirectiveIntermediateNode : IntermediateNode
    {
        private RazorDiagnosticCollection _diagnostics;

        public override ItemCollection Annotations { get; }

        public override RazorDiagnosticCollection Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new DefaultRazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public override IntermediateNodeCollection Children { get; } = new DefaultIntermediateNodeCollection();

        public override SourceSpan? Source { get; set; }

        public override bool HasDiagnostics => _diagnostics != null && _diagnostics.Count > 0;

        public string Name { get; set; }

        public IEnumerable<DirectiveTokenIntermediateNode> Tokens => Children.OfType<DirectiveTokenIntermediateNode>();

        public DirectiveDescriptor Descriptor { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            visitor.VisitMalformedDirective(this);
        }
    }
}