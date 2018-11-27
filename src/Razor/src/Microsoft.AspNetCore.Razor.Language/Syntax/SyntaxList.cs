// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal abstract class SyntaxList : SyntaxNode
    {
        internal SyntaxList(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
            : base(green, parent, position)
        {
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal class WithTwoChildren : SyntaxList
        {
            private SyntaxNode _child0;
            private SyntaxNode _child1;

            internal WithTwoChildren(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
                : base(green, parent, position)
            {
            }

            internal override SyntaxNode GetNodeSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return GetRedElement(ref _child0, 0);
                    case 1:
                        return GetRedElement(ref _child1, 1);
                    default:
                        return null;
                }
            }

            internal override SyntaxNode GetCachedSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    default:
                        return null;
                }
            }
        }

        internal class WithThreeChildren : SyntaxList
        {
            private SyntaxNode _child0;
            private SyntaxNode _child1;
            private SyntaxNode _child2;

            internal WithThreeChildren(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
                : base(green, parent, position)
            {
            }

            internal override SyntaxNode GetNodeSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return GetRedElement(ref _child0, 0);
                    case 1:
                        return GetRedElement(ref _child1, 1);
                    case 2:
                        return GetRedElement(ref _child2, 2);
                    default:
                        return null;
                }
            }

            internal override SyntaxNode GetCachedSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    case 2:
                        return _child2;
                    default:
                        return null;
                }
            }
        }

        internal class WithManyChildren : SyntaxList
        {
            private readonly ArrayElement<SyntaxNode>[] _children;

            internal WithManyChildren(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
                : base(green, parent, position)
            {
                _children = new ArrayElement<SyntaxNode>[green.SlotCount];
            }

            internal override SyntaxNode GetNodeSlot(int index)
            {
                return this.GetRedElement(ref _children[index].Value, index);
            }

            internal override SyntaxNode GetCachedSlot(int index)
            {
                return _children[index];
            }
        }
    }
}
