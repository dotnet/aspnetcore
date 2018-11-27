// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public abstract class IntermediateNode
    {
        private ItemCollection _annotations;
        private RazorDiagnosticCollection _diagnostics;

        public ItemCollection Annotations
        {
            get
            {
                if (_annotations == null)
                {
                    _annotations = new ItemCollection();
                }

                return _annotations;
            }
        }

        public abstract IntermediateNodeCollection Children { get; }

        public RazorDiagnosticCollection Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new RazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public bool HasDiagnostics => _diagnostics != null && _diagnostics.Count > 0;

        public SourceSpan? Source { get; set; }
        
        public abstract void Accept(IntermediateNodeVisitor visitor);
    }
}
