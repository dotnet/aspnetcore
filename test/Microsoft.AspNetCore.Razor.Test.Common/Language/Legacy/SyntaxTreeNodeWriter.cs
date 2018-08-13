// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;

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
                foreach (var descriptor in tagHelperBlock.Binding?.Descriptors ?? Array.Empty<TagHelperDescriptor>())
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

                    if (attribute.Value != null)
                    {
                        Depth++;
                        WriteNewLine();
                        // Recursively render attribute value
                        VisitNode(attribute.Value);
                        Depth--;
                    }
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
            if (span.SyntaxNode != null)
            {
                WriteSyntaxNode(span.SyntaxNode.CreateRed(null, span.Start.AbsoluteIndex));
                return;
            }

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
            Write($"Tokens:{span.Tokens.Count}");

            // Write tokens
            Depth++;
            foreach (var token in span.Tokens)
            {
                WriteNewLine();
                WriteIndent();
                WriteToken(token);
            }
            Depth--;
        }

        private void WriteSyntaxNode(SyntaxNode syntaxNode)
        {
            WriteIndent();
            Write($"{typeof(SyntaxKind).Name}.{syntaxNode.Kind}");
            WriteSeparator();
            Write($"[{syntaxNode.ToFullString()}]");
            WriteSeparator();
            Write($"[{syntaxNode.Position}..{syntaxNode.EndPosition})");
            WriteSeparator();
            Write($"FullWidth: {syntaxNode.FullWidth}");
            WriteSeparator();
            Write($"Slots: {syntaxNode.SlotCount}");

            // Write tokens
            Depth++;
            for (var i = 0; i < syntaxNode.SlotCount; i++)
            {
                var slot = syntaxNode.GetNodeSlot(i);
                if (slot == null)
                {
                    continue;
                }

                WriteNewLine();
                if (slot.IsList || !(slot is SyntaxToken syntaxToken))
                {
                    WriteSyntaxNode(slot);
                    continue;
                }

                WriteSyntaxToken(syntaxToken);
            }
            Depth--;
        }

        protected void WriteSyntaxToken(SyntaxToken syntaxToken)
        {
            WriteIndent();
            var diagnostics = syntaxToken.GetDiagnostics();
            var tokenString = $"{typeof(SyntaxKind).Name}.{syntaxToken.Kind};[{syntaxToken.Text}];{string.Join(", ", diagnostics.Select(diagnostic => diagnostic.Id + diagnostic.Span))}";
            Write(tokenString);
        }

        protected void WriteToken(IToken token)
        {
            var tokenType = string.Empty;
            IEnumerable<RazorDiagnostic> diagnostics = RazorDiagnostic.EmptyArray;

            if (token is HtmlToken htmlToken)
            {
                tokenType = $"{htmlToken.Type.GetType().Name}.{htmlToken.Type}";
                diagnostics = htmlToken.Errors;
            }
            else if (token is CSharpToken csharpToken)
            {
                tokenType = $"{csharpToken.Type.GetType().Name}.{csharpToken.Type}";
                diagnostics = csharpToken.Errors;
            }

            var tokenString = $"{tokenType};[{token.Content}];{string.Join(", ", diagnostics.Select(diagnostic => diagnostic.Id + diagnostic.Span))}";
            Write(tokenString);
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