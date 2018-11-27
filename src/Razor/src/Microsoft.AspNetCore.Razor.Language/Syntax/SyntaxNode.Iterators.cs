// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal abstract partial class SyntaxNode
    {
        private IEnumerable<SyntaxNode> DescendantNodesImpl(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren, bool includeSelf)
        {
            if (includeSelf && IsInSpan(in span, FullSpan))
            {
                yield return this;
            }

            using (var stack = new ChildSyntaxListEnumeratorStack(this, descendIntoChildren))
            {
                while (stack.IsNotEmpty)
                {
                    var nodeValue = stack.TryGetNextAsNodeInSpan(in span);
                    if (nodeValue != null)
                    {
                        // PERF: Push before yield return so that "nodeValue" is 'dead' after the yield
                        // and therefore doesn't need to be stored in the iterator state machine. This
                        // saves a field.
                        stack.PushChildren(nodeValue, descendIntoChildren);

                        yield return nodeValue;
                    }
                }
            }
        }

        private static bool IsInSpan(in TextSpan span, TextSpan childSpan)
        {
            return span.OverlapsWith(childSpan)
                // special case for zero-width tokens (OverlapsWith never returns true for these)
                || (childSpan.Length == 0 && span.IntersectsWith(childSpan));
        }

        private struct ChildSyntaxListEnumeratorStack : IDisposable
        {
            private static readonly ObjectPool<ChildSyntaxList.Enumerator[]> StackPool = new ObjectPool<ChildSyntaxList.Enumerator[]>(() => new ChildSyntaxList.Enumerator[16]);

            private ChildSyntaxList.Enumerator[] _stack;
            private int _stackPtr;

            public ChildSyntaxListEnumeratorStack(SyntaxNode startingNode, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(startingNode))
                {
                    _stack = StackPool.Allocate();
                    _stackPtr = 0;
                    _stack[0].InitializeFrom(startingNode);
                }
                else
                {
                    _stack = null;
                    _stackPtr = -1;
                }
            }

            public bool IsNotEmpty { get { return _stackPtr >= 0; } }

            public bool TryGetNextInSpan(in TextSpan span, out SyntaxNode value)
            {
                while (_stack[_stackPtr].TryMoveNextAndGetCurrent(out value))
                {
                    if (IsInSpan(in span, value.FullSpan))
                    {
                        return true;
                    }
                }

                _stackPtr--;
                return false;
            }

            public SyntaxNode TryGetNextAsNodeInSpan(in TextSpan span)
            {
                SyntaxNode nodeValue;
                while ((nodeValue = _stack[_stackPtr].TryMoveNextAndGetCurrentAsNode()) != null)
                {
                    if (IsInSpan(in span, nodeValue.FullSpan))
                    {
                        return nodeValue;
                    }
                }

                _stackPtr--;
                return null;
            }

            public void PushChildren(SyntaxNode node)
            {
                if (++_stackPtr >= _stack.Length)
                {
                    // Geometric growth
                    Array.Resize(ref _stack, checked(_stackPtr * 2));
                }

                _stack[_stackPtr].InitializeFrom(node);
            }

            public void PushChildren(SyntaxNode node, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(node))
                {
                    PushChildren(node);
                }
            }

            public void Dispose()
            {
                // Return only reasonably-sized stacks to the pool.
                if (_stack?.Length < 256)
                {
                    Array.Clear(_stack, 0, _stack.Length);
                    StackPool.Free(_stack);
                }
            }
        }
    }
}
