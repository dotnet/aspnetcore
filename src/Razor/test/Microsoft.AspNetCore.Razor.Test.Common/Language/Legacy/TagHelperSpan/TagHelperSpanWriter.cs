// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperSpanWriter
    {
        private readonly RazorSyntaxTree _syntaxTree;
        private readonly TextWriter _writer;

        public TagHelperSpanWriter(TextWriter writer, RazorSyntaxTree syntaxTree)
        {
            _writer = writer;
            _syntaxTree = syntaxTree;
        }

        public virtual void Visit()
        {
            var tagHelperSpans = _syntaxTree.GetTagHelperSpans();
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
    }
}
