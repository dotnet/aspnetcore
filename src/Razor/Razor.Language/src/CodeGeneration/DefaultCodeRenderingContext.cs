
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DefaultCodeRenderingContext : CodeRenderingContext
    {
        private readonly Stack<IntermediateNode> _ancestors;
        private readonly RazorCodeDocument _codeDocument;
        private readonly DocumentIntermediateNode _documentNode;
        private readonly List<ScopeInternal> _scopes;

        public DefaultCodeRenderingContext(
            CodeWriter codeWriter,
            IntermediateNodeWriter nodeWriter,
            RazorCodeDocument codeDocument,
            DocumentIntermediateNode documentNode,
            RazorCodeGenerationOptions options)
        {
            if (codeWriter == null)
            {
                throw new ArgumentNullException(nameof(codeWriter));
            }

            if (nodeWriter == null)
            {
                throw new ArgumentNullException(nameof(nodeWriter));
            }

            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (documentNode == null)
            {
                throw new ArgumentNullException(nameof(documentNode));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            CodeWriter = codeWriter;
            _codeDocument = codeDocument;
            _documentNode = documentNode;
            Options = options;

            _ancestors = new Stack<IntermediateNode>();
            Diagnostics = new RazorDiagnosticCollection();
            Items = new ItemCollection();
            SourceMappings = new List<SourceMapping>();

            var diagnostics = _documentNode.GetAllDiagnostics();
            for (var i = 0; i < diagnostics.Count; i++)
            {
                Diagnostics.Add(diagnostics[i]);
            }

            var newLineString = codeDocument.Items[NewLineString];
            if (newLineString != null)
            {
                // Set new line character to a specific string regardless of platform, for testing purposes.
                codeWriter.NewLine = (string)newLineString;
            }

            Items[NewLineString] = codeDocument.Items[NewLineString];
            Items[SuppressUniqueIds] = codeDocument.Items[SuppressUniqueIds];

            _scopes = new List<ScopeInternal>();
            _scopes.Add(new ScopeInternal(nodeWriter));
        }

        // This will be initialized by the document writer when the context is 'live'.
        public IntermediateNodeVisitor Visitor { get; set; }

        public override IEnumerable<IntermediateNode> Ancestors => _ancestors;

        internal Stack<IntermediateNode> AncestorsInternal => _ancestors;

        public override CodeWriter CodeWriter { get; }

        public override RazorDiagnosticCollection Diagnostics { get; }

        public override string DocumentKind { get; }

        public override ItemCollection Items { get; }

        public List<SourceMapping> SourceMappings { get; }

        public override IntermediateNodeWriter NodeWriter => Current.Writer;

        public override RazorCodeGenerationOptions Options { get; }

        public override IntermediateNode Parent => _ancestors.Count == 0 ? null : _ancestors.Peek();

        public override RazorSourceDocument SourceDocument => _codeDocument.Source;

        private ScopeInternal Current => _scopes[_scopes.Count - 1];

        public override void AddSourceMappingFor(IntermediateNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.Source == null)
            {
                return;
            }

            if (SourceDocument.FilePath != null &&
                !string.Equals(SourceDocument.FilePath, node.Source.Value.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                // We don't want to generate line mappings for imports.
                return;
            }

            var source = node.Source.Value;
            var generatedLocation = new SourceSpan(CodeWriter.Location, source.Length);
            var sourceMapping = new SourceMapping(source, generatedLocation);

            SourceMappings.Add(sourceMapping);
        }

        public override void RenderChildren(IntermediateNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            _ancestors.Push(node);

            for (var i = 0; i < node.Children.Count; i++)
            {
                Visitor.Visit(node.Children[i]);
            }

            _ancestors.Pop();
        }

        public override void RenderChildren(IntermediateNode node, IntermediateNodeWriter writer)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            _scopes.Add(new ScopeInternal(writer));
            _ancestors.Push(node);

            for (var i = 0; i < node.Children.Count; i++)
            {
                Visitor.Visit(node.Children[i]);
            }

            _ancestors.Pop();
            _scopes.RemoveAt(_scopes.Count - 1);
        }

        public override void RenderNode(IntermediateNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Visitor.Visit(node);
        }

        public override void RenderNode(IntermediateNode node, IntermediateNodeWriter writer)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            _scopes.Add(new ScopeInternal(writer));

            Visitor.Visit(node);

            _scopes.RemoveAt(_scopes.Count - 1);
        }

        private struct ScopeInternal
        {
            public ScopeInternal(IntermediateNodeWriter writer)
            {
                Writer = writer;
            }

            public IntermediateNodeWriter Writer { get; }
        }
    }
}
