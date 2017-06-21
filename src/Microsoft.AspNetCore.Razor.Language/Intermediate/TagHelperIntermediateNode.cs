// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class TagHelperIntermediateNode : IntermediateNode
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
                    _diagnostics = new DefaultRazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public override IntermediateNodeCollection Children { get; } = new DefaultIntermediateNodeCollection();

        public override SourceSpan? Source { get; set; }

        public override bool HasDiagnostics => _diagnostics != null && _diagnostics.Count > 0;

        public string TagName { get; set; }

        public TagMode TagMode { get; set; }

        public ICollection<TagHelperDescriptor> TagHelpers { get; } = new List<TagHelperDescriptor>();

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitTagHelper(this);
        }
    }
}
