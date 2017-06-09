// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class DocumentIRNode : RazorIRNode
    {
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

        public override RazorIRNodeCollection Children { get; } = new DefaultIRNodeCollection();

        public string DocumentKind { get; set; }

        public RazorCodeGenerationOptions Options { get; set; }

        public override SourceSpan? Source { get; set; }

        public CodeTarget Target { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitDocument(this);
        }
    }
}
