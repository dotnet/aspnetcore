// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public class CSharpRenderingContext
    {
        private CSharpRenderingConventions _renderingConventions;

        internal ICollection<DirectiveDescriptor> Directives { get; set; }

        internal Func<string> IdGenerator { get; set; } = () => Guid.NewGuid().ToString("N");

        internal List<LineMapping> LineMappings { get; } = new List<LineMapping>();

        public CSharpCodeWriter Writer { get; set; }

        internal CSharpRenderingConventions RenderingConventions
        {
            get
            {
                if (_renderingConventions == null)
                {
                    _renderingConventions = new CSharpRenderingConventions(Writer);
                }

                return _renderingConventions;
            }
            set
            {
                _renderingConventions = value;
            }
        }

        internal IList<RazorDiagnostic> Diagnostics { get; } = new List<RazorDiagnostic>();

        internal RazorSourceDocument SourceDocument { get; set; }

        internal RazorParserOptions Options { get; set; }

        internal TagHelperRenderingContext TagHelperRenderingContext { get; set; }

        internal Action<RazorIRNode> RenderChildren { get; set; }
    }
}
