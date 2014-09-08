// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeTreeBuilder
    {
        private readonly Stack<ChunkBlock> _blockChain;
        private Chunk _lastChunk;

        public CodeTreeBuilder()
        {
            CodeTree = new CodeTree();
            _blockChain = new Stack<ChunkBlock>();
        }

        public CodeTree CodeTree { get; private set; }

        public void AddChunk(Chunk chunk, SyntaxTreeNode association, bool topLevel = false)
        {
            _lastChunk = chunk;

            chunk.Start = association.Start;
            chunk.Association = association;

            // If we're not in the middle of a chunk block
            if (_blockChain.Count == 0 || topLevel == true)
            {
                CodeTree.Chunks.Add(chunk);
            }
            else
            {
                _blockChain.Peek().Children.Add(chunk);
            }
        }

        public void AddLiteralChunk(string literal, SyntaxTreeNode association)
        {
            // If the previous chunk was also a LiteralChunk, append the content of the current node to the previous one.
            var literalChunk = _lastChunk as LiteralChunk;
            if (literalChunk != null)
            {
                // Literal chunks are always associated with Spans
                var lastSpan = (Span)literalChunk.Association;
                var currentSpan = (Span)association;

                var builder = new SpanBuilder(lastSpan);
                foreach (var symbol in currentSpan.Symbols)
                {
                    builder.Accept(symbol);
                }

                literalChunk.Association = builder.Build();
                literalChunk.Text += literal;
            }
            else
            {
                AddChunk(new LiteralChunk
                {
                    Text = literal,
                }, association);
            }
        }

        public void AddExpressionChunk(string expression, SyntaxTreeNode association)
        {
            AddChunk(new ExpressionChunk
            {
                Code = expression
            }, association);
        }

        public void AddStatementChunk(string code, SyntaxTreeNode association)
        {
            AddChunk(new StatementChunk
            {
                Code = code,
            }, association);
        }

        public void AddUsingChunk(string usingNamespace, SyntaxTreeNode association)
        {
            AddChunk(new UsingChunk
            {
                Namespace = usingNamespace,
            }, association, topLevel: true);
        }

        public void AddTypeMemberChunk(string code, SyntaxTreeNode association)
        {
            AddChunk(new TypeMemberChunk
            {
                Code = code,
            }, association, topLevel: true);
        }

        public void AddLiteralCodeAttributeChunk(string code, SyntaxTreeNode association)
        {
            AddChunk(new LiteralCodeAttributeChunk
            {
                Code = code,
            }, association);
        }

        public void AddResolveUrlChunk(string url, SyntaxTreeNode association)
        {
            AddChunk(new ResolveUrlChunk
            {
                Url = url
            }, association);
        }

        public void AddSetLayoutChunk(string layout, SyntaxTreeNode association)
        {
            AddChunk(new SetLayoutChunk
            {
                Layout = layout
            }, association);
        }

        public void AddSetBaseTypeChunk(string typeName, SyntaxTreeNode association)
        {
            AddChunk(new SetBaseTypeChunk
            {
                TypeName = typeName.Trim()
            }, association, topLevel: true);
        }

        public void AddSessionStateChunk(string value, SyntaxTreeNode association)
        {
            AddChunk(new SessionStateChunk
            {
                Value = value
            }, association, topLevel: true);
        }

        public T StartChunkBlock<T>(SyntaxTreeNode association) where T : ChunkBlock, new()
        {
            return StartChunkBlock<T>(association, topLevel: false);
        }

        public T StartChunkBlock<T>(SyntaxTreeNode association, bool topLevel) where T : ChunkBlock, new()
        {
            var chunk = new T();

            AddChunk(chunk, association, topLevel);

            _blockChain.Push(chunk);

            return chunk;
        }

        public void EndChunkBlock()
        {
            _lastChunk = _blockChain.Pop();
        }
    }
}
