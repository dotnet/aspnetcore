// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class IntermediateToken : IntermediateNode
    {
        private RazorDiagnosticCollection _diagnostics;
        private ItemCollection _annotations;

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
                    _diagnostics = new DefaultRazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public override IntermediateNodeCollection Children => ReadOnlyIntermediateNodeCollection.Instance;

        public string Content { get; set; }

        public bool IsCSharp => Kind == TokenKind.CSharp;

        public bool IsHtml => Kind == TokenKind.Html;

        public TokenKind Kind { get; set; } = TokenKind.Unknown;

        public override SourceSpan? Source { get; set; }

        public override bool HasDiagnostics => _diagnostics != null && _diagnostics.Count > 0;

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitToken(this);
        }

        public enum TokenKind
        {
            Unknown,
            CSharp,
            Html,
        }
    }
}


