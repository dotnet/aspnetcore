// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class SyntaxTreeNodeWriter : ParserVisitor
    {
        private readonly TextWriter _writer;

        public int Depth { get; set; }

        public SyntaxTreeNodeWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public override void VisitDefault(Block block)
        {
            WriteBlock(block);
        }

        public override void VisitDefault(Span span)
        {
            WriteSpan(span);
        }

        public override void VisitTagHelperBlock(TagHelperChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
            
            if (block is TagHelperBlock tagHelperBlock)
            {
                // Write tag name
                WriteSeparator();
                Write(tagHelperBlock.TagName);

                // Write descriptors
                foreach (var descriptor in tagHelperBlock.Binding.Descriptors)
                {
                    WriteSeparator();

                    // Get the type name without the namespace.
                    var typeName = descriptor.Name.Substring(descriptor.Name.LastIndexOf('.') + 1);
                    Write(typeName);
                }

                // Write tag mode, start tag and end tag
                Depth++;
                WriteNewLine();
                WriteIndent();
                Write(tagHelperBlock.TagMode);
                WriteSeparator();
                Write(GetNodeContent(tagHelperBlock.SourceStartTag));
                if (tagHelperBlock.SourceEndTag != null)
                {
                    Write(" ... ");
                    Write(GetNodeContent(tagHelperBlock.SourceEndTag));
                }

                // Write attributes
                foreach (var attribute in tagHelperBlock.Attributes)
                {
                    WriteNewLine();
                    WriteIndent();
                    Write(attribute.Name);
                    WriteSeparator();
                    Write(attribute.AttributeStructure);

                    Depth++;
                    WriteNewLine();
                    // Recursively render attribute value
                    VisitNode(attribute.Value);
                    Depth--;
                }
                Depth--;
            }
        }

        public override void VisitAttributeBlock(AttributeBlockChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
        }

        public override void VisitCommentBlock(RazorCommentChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
        }

        public override void VisitDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
        }

        public override void VisitDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
        }

        public override void VisitExpressionBlock(ExpressionChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
        }

        public override void VisitTemplateBlock(TemplateBlockChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
        }

        public override void VisitMarkupSpan(MarkupChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitAddTagHelperSpan(AddTagHelperChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitExpressionSpan(ExpressionChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitImportSpan(AddImportChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitTagHelperPrefixDirectiveSpan(TagHelperPrefixDirectiveChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitStatementSpan(StatementChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitDirectiveToken(DirectiveTokenChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        protected void WriteBlock(Block block)
        {
            WriteIndent();
            Write($"{block.Type} block");
            WriteSeparator();
            Write($"Gen<{block.ChunkGenerator}>");
            WriteSeparator();
            Write(block.Length);
            WriteSeparator();
            WriteSourceLocation(block.Start);
        }

        protected void WriteSpan(Span span)
        {
            WriteIndent();
            Write($"{span.Kind} span");
            WriteSeparator();
            Write($"Gen<{span.ChunkGenerator}>");
            WriteSeparator();
            Write($"[{span.Content}]");
            WriteSeparator();
            Write(span.EditHandler);
            WriteSeparator();
            WriteSourceLocation(span.Start);
            WriteSeparator();
            Write($"Symbols:{span.Symbols.Count}");

            // Write symbols
            Depth++;
            foreach (var symbol in span.Symbols)
            {
                WriteNewLine();
                WriteIndent();
                WriteSymbol(symbol);
            }
            Depth--;
        }

        protected void WriteSymbol(ISymbol symbol)
        {
            var symbolType = string.Empty;
            IEnumerable<RazorDiagnostic> diagnostics = RazorDiagnostic.EmptyArray;

            if (symbol is HtmlSymbol htmlSymbol)
            {
                symbolType = $"{htmlSymbol.Type.GetType().Name}.{htmlSymbol.Type}";
                diagnostics = htmlSymbol.Errors;
            }
            else if (symbol is CSharpSymbol csharpSymbol)
            {
                symbolType = $"{csharpSymbol.Type.GetType().Name}.{csharpSymbol.Type}";
                diagnostics = csharpSymbol.Errors;
            }

            var symbolString = $"{symbolType};[{symbol.Content}];{string.Join(", ", diagnostics.Select(diagnostic => diagnostic.Id + diagnostic.Span))}";
            Write(symbolString);
        }

        protected void WriteSourceLocation(SourceLocation location)
        {
            Write(location);
        }

        protected void WriteLocationTaggedString(LocationTagged<string> item)
        {
            Write(item.ToString("F", null));
        }

        protected void WriteIndent()
        {
            for (var i = 0; i < Depth; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    Write(' ');
                }
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
            if (value is string stringValue)
            {
                stringValue = stringValue.Replace("\r\n", "LF");
                _writer.Write(stringValue);
                return;
            }

            _writer.Write(value);
        }

        private string GetNodeContent(SyntaxTreeNode node)
        {
            if (node is Span span)
            {
                return span.Content;
            }
            else if (node is Block block)
            {
                var content = string.Empty;
                foreach (var child in block.Children)
                {
                    content += GetNodeContent(child);
                }

                return content;
            }

            return string.Empty;
        }

        private void VisitNode(SyntaxTreeNode node)
        {
            Visit(node);

            if (node is Block block)
            {
                Depth++;
                foreach (var child in block.Children)
                {
                    WriteNewLine();
                    VisitNode(child);
                }
                Depth--;
            }
        }
    }
}