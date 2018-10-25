// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal readonly struct SyntaxList<TNode>
        where TNode : GreenNode
    {
        private readonly GreenNode _node;

        public SyntaxList(GreenNode node)
        {
            _node = node;
        }

        public GreenNode Node
        {
            get
            {
                return ((GreenNode)_node);
            }
        }

        public int Count
        {
            get
            {
                return (_node == null) ? 0 : _node.IsList ? _node.SlotCount : 1;
            }
        }

        public TNode Last
        {
            get
            {
                var node = _node;
                if (node.IsList)
                {
                    return ((TNode)node.GetSlot(node.SlotCount - 1));
                }

                return ((TNode)node);
            }
        }

        /* Not Implemented: Default */
        public TNode this[int index]
        {
            get
            {
                var node = _node;
                if (node.IsList)
                {
                    return ((TNode)node.GetSlot(index));
                }

                Debug.Assert(index == 0);
                return ((TNode)node);
            }
        }

        public GreenNode ItemUntyped(int index)
        {
            var node = _node;
            if (node.IsList)
            {
                return node.GetSlot(index);
            }

            Debug.Assert(index == 0);
            return node;
        }

        public bool Any()
        {
            return _node != null;
        }

        public bool Any(SyntaxKind kind)
        {
            for (var i = 0; i < Count; i++)
            {
                var element = ItemUntyped(i);
                if ((element.Kind == kind))
                {
                    return true;
                }
            }

            return false;
        }

        public TNode[] Nodes
        {
            get
            {
                var arr = new TNode[Count];
                for (var i = 0; i < Count; i++)
                {
                    arr[i] = this[i];
                }

                return arr;
            }
        }

        public static bool operator ==(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return (left._node == right._node);
        }

        public static bool operator !=(SyntaxList<TNode> left, SyntaxList<TNode> right)
        {
            return !(left._node == right._node);
        }

        public override bool Equals(object obj)
        {
            return (obj is SyntaxList<TNode> && (_node == ((SyntaxList<TNode>)obj)._node));
        }

        public override int GetHashCode()
        {
            return _node != null ? _node.GetHashCode() : 0;
        }

        public static implicit operator SyntaxList<TNode>(TNode node)
        {
            return new SyntaxList<TNode>(node);
        }

        public static implicit operator SyntaxList<GreenNode>(SyntaxList<TNode> nodes)
        {
            return new SyntaxList<GreenNode>(nodes._node);
        }
    }
}
