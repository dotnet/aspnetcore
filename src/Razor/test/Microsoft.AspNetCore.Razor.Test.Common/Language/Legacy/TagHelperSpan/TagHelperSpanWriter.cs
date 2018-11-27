// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperSpanWriter
    {
        private readonly string _filePath;
        private readonly TextWriter _writer;

        public TagHelperSpanWriter(TextWriter writer, string filePath)
        {
            _writer = writer;
            _filePath = filePath;
        }

        public virtual void Visit(SyntaxTreeNode node)
        {
            if (!(node is Block block))
            {
                return;
            }

            var tagHelperSpans = GetTagHelperSpans(block, _filePath);
            foreach (var span in tagHelperSpans)
            {
                VisitTagHelperSpan(span);
                WriteNewLine();
            }
        }

        public virtual void VisitTagHelperSpan(TagHelperSpanInternal span)
        {
            WriteTagHelperSpan(span);
        }

        protected void WriteTagHelperSpan(TagHelperSpanInternal span)
        {
            Write($"TagHelper span at {span.Span}");
            foreach (var tagHelper in span.TagHelpers)
            {
                WriteSeparator();

                // Get the type name without the namespace.
                var typeName = tagHelper.Name.Substring(tagHelper.Name.LastIndexOf('.') + 1);
                Write(typeName);
            }
        }

        protected void WriteSeparator()
        {
            Write(" - ");
        }

        protected void WriteNewLine()
        {
            _writer.WriteLine();
        }

        protected void Write(object value)
        {
            _writer.Write(value);
        }

        internal static IReadOnlyList<TagHelperSpanInternal> GetTagHelperSpans(Block root, string filePath)
        {
            // We don't care about the options and diagnostic here.
            var syntaxTree = RazorSyntaxTree.Create(
                root,
                TestRazorSourceDocument.Create(filePath: filePath),
                Array.Empty<RazorDiagnostic>(),
                RazorParserOptions.CreateDefault());

            return syntaxTree.GetTagHelperSpans();
        }
    }
}
