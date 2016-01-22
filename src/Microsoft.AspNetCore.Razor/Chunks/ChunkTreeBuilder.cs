// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Chunks
{
    public class ChunkTreeBuilder
    {
        private readonly Stack<ParentChunk> _parentStack;
        private Chunk _lastChunk;

        public ChunkTreeBuilder()
        {
            Root = new ChunkTree();
            _parentStack = new Stack<ParentChunk>();
            _parentStack.Push(Root);
        }

        public ParentChunk Current => _parentStack.Peek();

        public ChunkTree Root { get; }

        public void AddChunk(Chunk chunk, SyntaxTreeNode association, bool topLevel = false)
        {
            _lastChunk = chunk;

            chunk.Start = association.Start;
            chunk.Association = association;

            // If we're not in the middle of a parent chunk
            if (topLevel)
            {
                Root.Children.Add(chunk);
            }
            else
            {
                Current.Children.Add(chunk);
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
            ParentLiteralChunk parentLiteralChunk;

            // We try to join literal chunks where possible, so that we have fewer 'writes' in the generated code.
            //
            // Possible cases here:
            //  - We just added a LiteralChunk and we need to add another - so merge them into ParentLiteralChunk.
            //  - We have a ParentLiteralChunk - merge the new chunk into it.
            //  - We just added something <else> - just add the LiteralChunk like normal.
            if (_lastChunk is LiteralChunk)
            {
                parentLiteralChunk = new ParentLiteralChunk()
                {
                    Start = _lastChunk.Start,
                };

                parentLiteralChunk.Children.Add(_lastChunk);
                parentLiteralChunk.Children.Add(new LiteralChunk
                {
                    Association = association,
                    Start = association.Start,
                    Text = literal,
                });

                Debug.Assert(Current.Children[Current.Children.Count - 1] == _lastChunk);
                Current.Children.RemoveAt(Current.Children.Count - 1);
                Current.Children.Add(parentLiteralChunk);
                _lastChunk = parentLiteralChunk;
            }
            else if ((parentLiteralChunk = _lastChunk as ParentLiteralChunk) != null)
            {
                parentLiteralChunk.Children.Add(new LiteralChunk
                {
                    Association = association,
                    Start = association.Start,
                    Text = literal,
                });
                _lastChunk = parentLiteralChunk;
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
