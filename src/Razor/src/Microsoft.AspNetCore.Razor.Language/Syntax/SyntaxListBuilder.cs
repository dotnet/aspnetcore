// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal class SyntaxListBuilder
    {
        private ArrayElement<GreenNode>[] _nodes;

        public int Count { get; private set; }

        public SyntaxListBuilder(int size)
        {
            _nodes = new ArrayElement<GreenNode>[size];
        }

        public void Clear()
        {
            Count = 0;
        }

        public void Add(SyntaxNode item)
        {
            AddInternal(item.Green);
        }

        internal void AddInternal(GreenNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (_nodes == null || Count >= _nodes.Length)
            {
                Grow(Count == 0 ? 8 : _nodes.Length * 2);
            }

            _nodes[Count++].Value = item;
        }

        public void AddRange(SyntaxNode[] items)
        {
            AddRange(items, 0, items.Length);
        }

        public void AddRange(SyntaxNode[] items, int offset, int length)
        {
            if (_nodes == null || Count + length > _nodes.Length)
            {
                Grow(Count + length);
            }

            for (int i = offset, j = Count; i < offset + length; ++i, ++j)
            {
                _nodes[j].Value = items[i].Green;
            }

            var start = Count;
            Count += length;
            Validate(start, Count);
        }

        [Conditional("DEBUG")]
        private void Validate(int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                if (_nodes[i].Value == null)
                {
                    throw new ArgumentException("Cannot add a null node.");
                }
            }
        }

        public void AddRange(SyntaxList<SyntaxNode> list)
        {
            AddRange(list, 0, list.Count);
        }

        public void AddRange(SyntaxList<SyntaxNode> list, int offset, int count)
        {
            if (_nodes == null || Count + count > _nodes.Length)
            {
                Grow(Count + count);
            }

            var dst = Count;
            for (int i = offset, limit = offset + count; i < limit; i++)
            {
                _nodes[dst].Value = list.ItemInternal(i).Green;
                dst++;
            }

            var start = Count;
            Count += count;
            Validate(start, Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
        {
            AddRange(list, 0, list.Count);
        }

        public void AddRange<TNode>(SyntaxList<TNode> list, int offset, int count) where TNode : SyntaxNode
        {
            AddRange(new SyntaxList<SyntaxNode>(list.Node), offset, count);
        }

        private void Grow(int size)
        {
            var tmp = new ArrayElement<GreenNode>[size];
            Array.Copy(_nodes, tmp, _nodes.Length);
            _nodes = tmp;
        }

        public bool Any(SyntaxKind kind)
        {
            for (var i = 0; i < Count; i++)
            {
                if (_nodes[i].Value.Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        internal GreenNode ToListNode()
        {
            switch (Count)
            {
                case 0:
                    return null;
                case 1:
                    return _nodes[0].Value;
                case 2:
                    return InternalSyntax.SyntaxList.List(_nodes[0].Value, _nodes[1].Value);
                case 3:
                    return InternalSyntax.SyntaxList.List(_nodes[0].Value, _nodes[1].Value, _nodes[2].Value);
                default:
                    var tmp = new ArrayElement<GreenNode>[Count];
                    for (var i = 0; i < Count; i++)
                    {
                        tmp[i].Value = _nodes[i].Value;
                    }

                    return InternalSyntax.SyntaxList.List(tmp);
            }
        }

        public static implicit operator SyntaxList<SyntaxNode>(SyntaxListBuilder builder)
        {
            if (builder == null)
            {
                return default(SyntaxList<SyntaxNode>);
            }

            return builder.ToList();
        }

        internal void RemoveLast()
        {
            Count -= 1;
            _nodes[Count] = default(ArrayElement<GreenNode>);
        }
    }
}
