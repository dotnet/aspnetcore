
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DefaultCodeRenderingContext : CodeRenderingContext
    {
        // Internal for unit testing
        internal DefaultCodeRenderingContext(
            CodeWriter codeWriter,
            IntermediateNodeWriter nodeWriter,
            RazorSourceDocument sourceDocument,
            RazorCodeGenerationOptions options)
            : this (codeWriter, nodeWriter, null, sourceDocument, options)
        {
        }

        public DefaultCodeRenderingContext(
            CodeWriter codeWriter,
            IntermediateNodeWriter nodeWriter,
            string documentKind,
            RazorSourceDocument sourceDocument,
            RazorCodeGenerationOptions options)
        {
            CodeWriter = codeWriter;
            NodeWriter = nodeWriter;
            DocumentKind = documentKind;
            SourceDocument = sourceDocument;
            Options = options;
            Diagnostics = new DefaultRazorDiagnosticCollection();
            Items = new DefaultItemCollection();
        }

        public override CodeWriter CodeWriter { get; }

        public override IntermediateNodeWriter NodeWriter { get; protected set; }

        public override RazorSourceDocument SourceDocument { get; }

        public override RazorCodeGenerationOptions Options { get; }

        public override RazorDiagnosticCollection Diagnostics { get; }

        public override ItemCollection Items { get; }

        public override string DocumentKind { get; }

        public override IntermediateNodeWriterScope Push(IntermediateNodeWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var scope = new IntermediateNodeWriterScope(this, NodeWriter);
            NodeWriter = writer;
            return scope;
        }
    }
}
