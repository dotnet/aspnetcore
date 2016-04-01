// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks
{
    public class ChunkTreeBuilder
    {
        private readonly Stack<ParentChunk> _parentStack;
        private Chunk _lastChunk;

        public ChunkTreeBuilder()
        {
            ChunkTree = new ChunkTree();
            _parentStack = new Stack<ParentChunk>();
        }

        public ChunkTree ChunkTree { get; private set; }

        public void AddChunk(Chunk chunk, SyntaxTreeNode association, bool topLevel = false)
        {
            _lastChunk = chunk;

            chunk.Start = association.Start;
            chunk.Association = association;

            // If we're not in the middle of a parent chunk
            if (_parentStack.Count == 0 || topLevel == true)
            {
                ChunkTree.Chunks.Add(chunk);
            }
            else
            {
                _parentStack.Peek().Children.Add(chunk);
            }
        }

        public void AddTagHelperPrefixDirectiveChunk(string prefix, SyntaxTreeNode association)
        {
            AddChunk(
                new TagHelperPrefixDirectiveChunk
                {
                    Prefix = prefix
                },
                association,
                topLevel: true);
        }

        public void AddAddTagHelperChunk(string lookupText, SyntaxTreeNode association)
        {
            AddChunk(new AddTagHelperChunk
            {
                LookupText = lookupText
            }, association, topLevel: true);
        }

        public void AddRemoveTagHelperChunk(string lookupText, SyntaxTreeNode association)
        {
            AddChunk(new RemoveTagHelperChunk
            {
                LookupText = lookupText
            }, association, topLevel: true);
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

        public void AddSetBaseTypeChunk(string typeName, SyntaxTreeNode association)
        {
            AddChunk(new SetBaseTypeChunk
            {
                TypeName = typeName.Trim()
            }, association, topLevel: true);
        }

        public T StartParentChunk<T>(SyntaxTreeNode association) where T : ParentChunk, new()
        {
            return StartParentChunk<T>(association, topLevel: false);
        }

        public T StartParentChunk<T>(SyntaxTreeNode association, bool topLevel) where T : ParentChunk, new()
        {
            var parentChunk = new T();

            return StartParentChunk<T>(parentChunk, association, topLevel);
        }

        public T StartParentChunk<T>(T parentChunk, SyntaxTreeNode association, bool topLevel) where T : ParentChunk
        {
            AddChunk(parentChunk, association, topLevel);

            _parentStack.Push(parentChunk);

            return parentChunk;
        }

        public void EndParentChunk()
        {
            _lastChunk = _parentStack.Pop();
        }
    }
}
