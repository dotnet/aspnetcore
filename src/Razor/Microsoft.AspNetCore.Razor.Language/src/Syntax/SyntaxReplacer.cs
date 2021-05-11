// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal static class SyntaxReplacer
    {
        internal static SyntaxNode Replace<TNode>(
            SyntaxNode root,
            IEnumerable<TNode> nodes = null,
            Func<TNode, TNode, SyntaxNode> computeReplacementNode = null)
            where TNode : SyntaxNode
        {
            var replacer = new Replacer<TNode>(nodes, computeReplacementNode);

            if (replacer.HasWork)
            {
                return replacer.Visit(root);
            }
            else
            {
                return root;
            }
        }

        internal static SyntaxNode ReplaceNodeInList(SyntaxNode root, SyntaxNode originalNode, IEnumerable<SyntaxNode> newNodes)
        {
            return new NodeListEditor(originalNode, newNodes, ListEditKind.Replace).Visit(root);
        }

        internal static SyntaxNode InsertNodeInList(SyntaxNode root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        {
            return new NodeListEditor(nodeInList, nodesToInsert, insertBefore ? ListEditKind.InsertBefore : ListEditKind.InsertAfter).Visit(root);
        }

        private class Replacer<TNode> : SyntaxRewriter where TNode : SyntaxNode
        {
            private readonly Func<TNode, TNode, SyntaxNode> _computeReplacementNode;
            private readonly HashSet<SyntaxNode> _nodeSet;
            private readonly HashSet<TextSpan> _spanSet;
            private readonly TextSpan _totalSpan;

            public Replacer(IEnumerable<TNode> nodes, Func<TNode, TNode, SyntaxNode> computeReplacementNode)
            {
                _computeReplacementNode = computeReplacementNode;
                _nodeSet = nodes != null ? new HashSet<SyntaxNode>(nodes) : new HashSet<SyntaxNode>();
                _spanSet = new HashSet<TextSpan>(_nodeSet.Select(n => n.FullSpan));
                _totalSpan = ComputeTotalSpan(_spanSet);
            }

            public bool HasWork => _nodeSet.Count > 0;

            public override SyntaxNode Visit(SyntaxNode node)
            {
                var rewritten = node;

                if (node != null)
                {
                    if (ShouldVisit(node.FullSpan))
                    {
                        rewritten = base.Visit(node);
                    }

                    if (_nodeSet.Contains(node) && _computeReplacementNode != null)
                    {
                        rewritten = _computeReplacementNode((TNode)node, (TNode)rewritten);
                    }
                }

                return rewritten;
            }

            private static TextSpan ComputeTotalSpan(IEnumerable<TextSpan> spans)
            {
                var first = true;
                var start = 0;
                var end = 0;

                foreach (var span in spans)
                {
                    if (first)
                    {
                        start = span.Start;
                        end = span.End;
                        first = false;
                    }
                    else
                    {
                        start = Math.Min(start, span.Start);
                        end = Math.Max(end, span.End);
                    }
                }

                return new TextSpan(start, end - start);
            }

            private bool ShouldVisit(TextSpan span)
            {
                // first do quick check against total span
                if (!span.IntersectsWith(_totalSpan))
                {
                    // if the node is outside the total span of the nodes to be replaced
                    // then we won't find any nodes to replace below it.
                    return false;
                }

                foreach (var s in _spanSet)
                {
                    if (span.IntersectsWith(s))
                    {
                        // node's full span intersects with at least one node to be replaced
                        // so we need to visit node's children to find it.
                        return true;
                    }
                }

                return false;
            }
        }

        private class NodeListEditor : SyntaxRewriter
        {
            private readonly TextSpan _elementSpan;
            private readonly SyntaxNode _originalNode;
            private readonly IEnumerable<SyntaxNode> _newNodes;
            private readonly ListEditKind _editKind;

            public NodeListEditor(
                SyntaxNode originalNode,
                IEnumerable<SyntaxNode> replacementNodes,
                ListEditKind editKind)
            {
                _elementSpan = originalNode.Span;
                _originalNode = originalNode;
                _newNodes = replacementNodes;
                _editKind = editKind;
            }

            private bool ShouldVisit(TextSpan span)
            {
                if (span.IntersectsWith(_elementSpan))
                {
                    // node's full span intersects with at least one node to be replaced
                    // so we need to visit node's children to find it.
                    return true;
                }

                return false;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == _originalNode)
                {
                    throw new InvalidOperationException("Expecting a list");
                }

                var rewritten = node;

                if (node != null)
                {
                    if (ShouldVisit(node.FullSpan))
                    {
                        rewritten = base.Visit(node);
                    }
                }

                return rewritten;
            }

            public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
            {
                if (_originalNode is TNode)
                {
                    var index = list.IndexOf((TNode)_originalNode);
                    if (index >= 0 && index < list.Count)
                    {
                        switch (_editKind)
                        {
                            case ListEditKind.Replace:
                                return list.ReplaceRange((TNode)_originalNode, _newNodes.Cast<TNode>());

                            case ListEditKind.InsertAfter:
                                return list.InsertRange(index + 1, _newNodes.Cast<TNode>());

                            case ListEditKind.InsertBefore:
                                return list.InsertRange(index, _newNodes.Cast<TNode>());
                        }
                    }
                }

                return base.VisitList<TNode>(list);
            }
        }

        private enum ListEditKind
        {
            InsertBefore,
            InsertAfter,
            Replace
        }
    }
}
