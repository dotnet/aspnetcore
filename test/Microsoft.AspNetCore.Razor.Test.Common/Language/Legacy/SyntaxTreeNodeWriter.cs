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
                WriteSeparator();
                Write(tagHelperBlock.TagName);
                WriteSeparator();
                Write(tagHelperBlock.TagMode);

                foreach (var descriptor in tagHelperBlock.Binding.Descriptors)
                {
                    WriteSeparator();

                    // Get the type name without the namespace.
                    var typeName = descriptor.Name.Substring(descriptor.Name.LastIndexOf('.') + 1);
                    Write(typeName);
                }
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

            var symbolString = $"{symbolType};[{symbol.Content}];{string.Join(",", diagnostics.Select(diagnostic => diagnostic.Id))}";
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
    }
}