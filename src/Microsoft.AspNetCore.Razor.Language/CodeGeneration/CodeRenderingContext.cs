// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class CodeRenderingContext
    {
        internal static readonly object NewLineString = "NewLineString";
        internal static readonly object SuppressUniqueIds = "SuppressUniqueIds";

        public static CodeRenderingContext Create(RazorCodeDocument codeDocument, RazorCodeGenerationOptions options)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            IntermediateNodeWriter nodeWriter;
            TagHelperWriter tagHelperWriter;

            if (options.DesignTime)
            {
                nodeWriter = new DesignTimeNodeWriter();
                tagHelperWriter = new DesignTimeTagHelperWriter();
            }
            else
            {
                nodeWriter = new RuntimeNodeWriter();
                tagHelperWriter = new RuntimeTagHelperWriter();
            }

            var documentKind = codeDocument.GetDocumentIntermediateNode()?.DocumentKind;
            var codeWriter = new CodeWriter();
            var context = new DefaultCodeRenderingContext(codeWriter, nodeWriter, documentKind, codeDocument.Source, options)
            {
                TagHelperWriter = tagHelperWriter
            };

            var newLineString = codeDocument.Items[NewLineString];
            if (newLineString != null)
            {
                // Set new line character to a specific string regardless of platform, for testing purposes.
                codeWriter.NewLine = (string)newLineString;
            }

            context.Items[SuppressUniqueIds] = codeDocument.Items[SuppressUniqueIds];

            return context;
        }

        public abstract CodeWriter CodeWriter { get; }

        public abstract IntermediateNodeWriter NodeWriter { get; protected set; }

        public abstract RazorSourceDocument SourceDocument { get; }

        public abstract RazorCodeGenerationOptions Options { get; }

        public abstract RazorDiagnosticCollection Diagnostics { get; }

        public abstract ItemCollection Items { get; }

        public abstract string DocumentKind { get; }

        public abstract IntermediateNodeWriterScope Push(IntermediateNodeWriter writer);

        public struct IntermediateNodeWriterScope : IDisposable
        {
            private readonly CodeRenderingContext _context;
            private readonly IntermediateNodeWriter _writer;

            public IntermediateNodeWriterScope(CodeRenderingContext context, IntermediateNodeWriter writer)
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
                _context.NodeWriter = _writer;
            }
        }


        // All bits below here are temporary

        #region Temporary TagHelper bits
        internal TagHelperWriter TagHelperWriter { get; set; }

        internal TagHelperRenderingContext TagHelperRenderingContext { get; set; }

        internal TagHelperWriterScope Push(TagHelperWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var scope = new TagHelperWriterScope(this, TagHelperWriter);
            TagHelperWriter = writer;
            return scope;
        }

        internal TagHelperRenderingContextScope Push(TagHelperRenderingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var scope = new TagHelperRenderingContextScope(this, TagHelperRenderingContext);
            TagHelperRenderingContext = context;
            return scope;
        }

        internal struct TagHelperWriterScope : IDisposable
        {
            private readonly CodeRenderingContext _context;
            private readonly TagHelperWriter _writer;

            public TagHelperWriterScope(CodeRenderingContext context, TagHelperWriter writer)
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

        internal struct TagHelperRenderingContextScope : IDisposable
        {
            private readonly CodeRenderingContext _context;
            private readonly TagHelperRenderingContext _renderingContext;

            public TagHelperRenderingContextScope(CodeRenderingContext context, TagHelperRenderingContext renderingContext)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                _context = context;
                _renderingContext = renderingContext;
            }

            public void Dispose()
            {
                _context.TagHelperRenderingContext = _renderingContext;
            }
        }

        #endregion
    }
}
