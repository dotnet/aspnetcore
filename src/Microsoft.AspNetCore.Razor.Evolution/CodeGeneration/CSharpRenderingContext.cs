// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class CSharpRenderingContext
    {
        private CSharpRenderingConventions _renderingConventions;

        public ICollection<DirectiveDescriptor> Directives { get; set; }

        public Func<string> IdGenerator { get; set; } = () => Guid.NewGuid().ToString("N");

        public List<LineMapping> LineMappings { get; } = new List<LineMapping>();

        public CSharpCodeWriter Writer { get; set; }

        public CSharpRenderingConventions RenderingConventions
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

        public ErrorSink ErrorSink { get; } = new ErrorSink();

        public RazorSourceDocument SourceDocument { get; set; }

        public RazorParserOptions Options { get; set; }

        public TagHelperRenderingContext TagHelperRenderingContext { get; set; }
    }
}
