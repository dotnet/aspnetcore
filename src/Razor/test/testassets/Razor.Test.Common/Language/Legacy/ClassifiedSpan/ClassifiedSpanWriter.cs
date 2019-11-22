// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class ClassifiedSpanWriter
    {
        private readonly string _filePath;
        private readonly TextWriter _writer;

        public ClassifiedSpanWriter(TextWriter writer, string filePath)
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
            var classifiedSpans = GetClassifiedSpans(block, _filePath);
            foreach (var span in classifiedSpans)
            {
                VisitClassifiedSpan(span);
                WriteNewLine();
            }
        }

        public virtual void VisitClassifiedSpan(ClassifiedSpanInternal span)
        {
            WriteClassifiedSpan(span);
        }

        protected void WriteClassifiedSpan(ClassifiedSpanInternal span)
        {
            Write($"{span.SpanKind} span at {span.Span} (Accepts:{span.AcceptedCharacters})");
            WriteSeparator();
            Write($"Parent: {span.BlockKind} block at {span.BlockSpan}");
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

        internal static IReadOnlyList<ClassifiedSpanInternal> GetClassifiedSpans(Block root, string filePath)
        {
            // We don't care about the options and diagnostic here.
            var syntaxTree = RazorSyntaxTree.Create(
                root,
                TestRazorSourceDocument.Create(filePath: filePath),
                Array.Empty<RazorDiagnostic>(),
                RazorParserOptions.CreateDefault());

            return syntaxTree.GetClassifiedSpans();
        }

        private static List<Span> Flatten(SyntaxTreeNode root)
        {
            var result = new List<Span>();
            AppendFlattenedSpans(root, result);
            return result;

            void AppendFlattenedSpans(SyntaxTreeNode node, List<Span> foundSpans)
            {
                if (node is Span spanNode)
                {
                    foundSpans.Add(spanNode);
                }
                else
                {
                    if (node is TagHelperBlock tagHelperNode)
                    {
                        // These aren't in document order, sort them first and then dig in
                        var attributeNodes = tagHelperNode.Attributes.Select(kvp => kvp.Value).Where(att => att != null).ToList();
                        attributeNodes.Sort((x, y) => x.Start.AbsoluteIndex.CompareTo(y.Start.AbsoluteIndex));

                        foreach (var attribute in attributeNodes)
                        {
                            AppendFlattenedSpans(attribute, foundSpans);
                        }
                    }

                    if (node is Block blockNode)
                    {
                        foreach (var child in blockNode.Children)
                        {
                            AppendFlattenedSpans(child, foundSpans);
                        }
                    }
                }
            }
        }
    }
}
