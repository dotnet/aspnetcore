// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
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

        internal RazorCodeDocument CodeDocument { get; set; }

        internal RazorSourceDocument SourceDocument => CodeDocument?.Source;

        internal RazorParserOptions Options { get; set; }

        internal TagHelperRenderingContext TagHelperRenderingContext { get; set; }

        internal Action<RazorIRNode> RenderChildren { get; set; }

        internal Action<RazorIRNode> RenderNode { get; set; }

        public BasicWriter BasicWriter { get; set; }

        public TagHelperWriter TagHelperWriter { get; set; }

        public void AddLineMappingFor(RazorIRNode node)
        {
            if (node.Source == null)
            {
                return;
            }

            if (SourceDocument.FileName != null &&
                !string.Equals(SourceDocument.FileName, node.Source.Value.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                // We don't want to generate line mappings for imports.
                return;
            }

            var source = node.Source.Value;

            var generatedLocation = new SourceSpan(Writer.GetCurrentSourceLocation(), source.Length);
            var lineMapping = new LineMapping(source, generatedLocation);

            LineMappings.Add(lineMapping);
        }

        public BasicWriterScope Push(BasicWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var scope = new BasicWriterScope(this, BasicWriter);
            BasicWriter = writer;
            return scope;
        }

        public TagHelperWriterScope Push(TagHelperWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var scope = new TagHelperWriterScope(this, TagHelperWriter);
            TagHelperWriter = writer;
            return scope;
        }

        public struct BasicWriterScope : IDisposable
        {
            private readonly CSharpRenderingContext _context;
            private readonly BasicWriter _writer;

            public BasicWriterScope(CSharpRenderingContext context, BasicWriter writer)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                _context = context;
                _writer = writer;
            }

            public void Dispose()
            {
                _context.BasicWriter = _writer;
            }
        }

        public struct TagHelperWriterScope : IDisposable
        {
            private readonly CSharpRenderingContext _context;
            private readonly TagHelperWriter _writer;

            public TagHelperWriterScope(CSharpRenderingContext context, TagHelperWriter writer)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                _context = context;
                _writer = writer;
            }

            public void Dispose()
            {
                _context.TagHelperWriter = _writer;
            }
        }
    }
}
