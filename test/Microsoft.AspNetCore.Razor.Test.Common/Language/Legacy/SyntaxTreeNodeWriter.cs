// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

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
            WriteSeparator();
            Write(chunkGenerator.Name);
            WriteSeparator();
            WriteLocationTaggedString(chunkGenerator.Prefix);
            WriteSeparator();
            WriteLocationTaggedString(chunkGenerator.Suffix);
        }

        public override void VisitCommentBlock(RazorCommentChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
        }

        public override void VisitDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
            WriteSeparator();
            Write(chunkGenerator.Descriptor.Directive);
            WriteSeparator();
            Write(chunkGenerator.Descriptor.Kind);
            WriteSeparator();
            Write(chunkGenerator.Descriptor.Usage);
        }

        public override void VisitDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunkGenerator, Block block)
        {
            WriteBlock(block);
            WriteSeparator();
            WriteLocationTaggedString(chunkGenerator.Prefix);
            WriteSeparator();
            WriteSourceLocation(chunkGenerator.ValueStart);
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
            WriteSeparator();
            Write(chunkGenerator.LookupText);
            WriteSeparator();
            Write(chunkGenerator.DirectiveText);
            WriteSeparator();
            Write(chunkGenerator.TypePattern);
            WriteSeparator();
            Write(chunkGenerator.AssemblyName);
        }

        public override void VisitExpressionSpan(ExpressionChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitImportSpan(AddImportChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
            WriteSeparator();
            Write(chunkGenerator.Namespace);
        }

        public override void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
            WriteSeparator();
            WriteLocationTaggedString(chunkGenerator.Prefix);
            WriteSeparator();
            WriteLocationTaggedString(chunkGenerator.Value);
        }

        public override void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
            WriteSeparator();
            Write(chunkGenerator.LookupText);
            WriteSeparator();
            Write(chunkGenerator.DirectiveText);
            WriteSeparator();
            Write(chunkGenerator.TypePattern);
            WriteSeparator();
            Write(chunkGenerator.AssemblyName);
        }

        public override void VisitTagHelperPrefixDirectiveSpan(TagHelperPrefixDirectiveChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
            WriteSeparator();
            Write(chunkGenerator.Prefix);
            WriteSeparator();
            Write(chunkGenerator.DirectiveText);
        }

        public override void VisitStatementSpan(StatementChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
        }

        public override void VisitDirectiveToken(DirectiveTokenChunkGenerator chunkGenerator, Span span)
        {
            WriteSpan(span);
            WriteSeparator();
            Write(chunkGenerator.Descriptor.Kind);
            WriteSeparator();
            Write(chunkGenerator.Descriptor.Name);
            WriteSeparator();
            Write($"Optional: {chunkGenerator.Descriptor.Optional}");
        }

        protected void WriteBlock(Block block)
        {
            WriteIndent();
            Write($"{block.Type} block");
            WriteSeparator();
            Write(block.ChunkGenerator.GetType().Name);
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
            Write(span.ChunkGenerator.GetType().Name);
            WriteSeparator();
            Write(span.EditHandler);
            WriteSeparator();
            Write(span.Content);
            WriteSeparator();
            WriteSourceLocation(span.Start);
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