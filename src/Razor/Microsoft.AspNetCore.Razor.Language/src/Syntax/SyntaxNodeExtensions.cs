// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal static class SyntaxNodeExtensions
    {
        // From http://dev.w3.org/html5/spec/Overview.html#elements-0
        private static readonly HashSet<string> VoidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "area",
            "base",
            "br",
            "col",
            "command",
            "embed",
            "hr",
            "img",
            "input",
            "keygen",
            "link",
            "meta",
            "param",
            "source",
            "track",
            "wbr"
        };

        public static TNode WithAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return (TNode)node.Green.SetAnnotations(annotations).CreateRed(node.Parent, node.Position);
        }

        public static object GetAnnotationValue<TNode>(this TNode node, string key) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var annotation = node.GetAnnotations().FirstOrDefault(n => n.Kind == key);
            return annotation?.Data;
        }

        public static TNode WithDiagnostics<TNode>(this TNode node, params RazorDiagnostic[] diagnostics) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return (TNode)node.Green.SetDiagnostics(diagnostics).CreateRed(node.Parent, node.Position);
        }

        public static TNode AppendDiagnostic<TNode>(this TNode node, params RazorDiagnostic[] diagnostics) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var existingDiagnostics = node.GetDiagnostics();
            var allDiagnostics = existingDiagnostics.Concat(diagnostics).ToArray();

            return (TNode)node.WithDiagnostics(allDiagnostics);
        }

        public static SourceLocation GetSourceLocation(this SyntaxNode node, RazorSourceDocument source)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            try
            {
                if (source.Length == 0)
                {
                    // Just a marker symbol
                    return new SourceLocation(source.FilePath, 0, 0, 0);
                }
                if (node.Position == source.Length)
                {
                    // E.g. Marker symbol at the end of the document
                    var lastPosition = source.Length - 1;
                    var endsWithLineBreak = ParserHelpers.IsNewLine(source[lastPosition]);
                    var lastLocation = source.Lines.GetLocation(lastPosition);
                    return new SourceLocation(
                        source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                        lastLocation.AbsoluteIndex + 1,
                        lastLocation.LineIndex + (endsWithLineBreak ? 1 : 0),
                        endsWithLineBreak ? 0 : lastLocation.CharacterIndex + 1);
                }

                var location = source.Lines.GetLocation(node.Position);
                return new SourceLocation(
                    source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                    location.AbsoluteIndex,
                    location.LineIndex,
                    location.CharacterIndex);
            }
            catch (IndexOutOfRangeException)
            {
                Debug.Assert(false, "Node position should stay within document length.");
                return new SourceLocation(source.FilePath, node.Position, 0, 0);
            }
        }

        public static SourceSpan GetSourceSpan(this SyntaxNode node, RazorSourceDocument source)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var location = node.GetSourceLocation(source);

            return new SourceSpan(location, node.FullWidth);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified nodes, tokens and trivia replaced.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="nodes">The nodes to be replaced.</param>
        /// <param name="computeReplacementNode">A function that computes a replacement node for the
        /// argument nodes. The first argument is the original node. The second argument is the same
        /// node potentially rewritten with replaced descendants.</param>
        public static TRoot ReplaceSyntax<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceCore(
                nodes: nodes, computeReplacementNode: computeReplacementNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <typeparam name="TNode">The type of the nodes being replaced.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="nodes">The nodes to be replaced; descendants of the root node.</param>
        /// <param name="computeReplacementNode">A function that computes a replacement node for the
        /// argument nodes. The first argument is the original node. The second argument is the same
        /// node potentially rewritten with replaced descendants.</param>
        public static TRoot ReplaceNodes<TRoot, TNode>(this TRoot root, IEnumerable<TNode> nodes, Func<TNode, TNode, SyntaxNode> computeReplacementNode)
            where TRoot : SyntaxNode
            where TNode : SyntaxNode
        {
            return (TRoot)root.ReplaceCore(nodes: nodes, computeReplacementNode: computeReplacementNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified old node replaced with a new node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root node of the tree of nodes.</param>
        /// <param name="oldNode">The node to be replaced; a descendant of the root node.</param>
        /// <param name="newNode">The new node to use in the new tree in place of the old node.</param>
        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, SyntaxNode newNode)
            where TRoot : SyntaxNode
        {
            if (oldNode == newNode)
            {
                return root;
            }

            return (TRoot)root.ReplaceCore(nodes: new[] { oldNode }, computeReplacementNode: (o, r) => newNode);
        }

        /// <summary>
        /// Creates a new tree of nodes with specified old node replaced with a new nodes.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="oldNode">The node to be replaced; a descendant of the root node and an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to use in the tree in place of the old node.</param>
        public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.ReplaceNodeInListCore(oldNode, newNodes);
        }

        /// <summary>
        /// Creates a new tree of nodes with new nodes inserted before the specified node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="nodeInList">The node to insert before; a descendant of the root node an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to insert into the tree immediately before the specified node.</param>
        public static TRoot InsertNodesBefore<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertNodesInListCore(nodeInList, newNodes, insertBefore: true);
        }

        /// <summary>
        /// Creates a new tree of nodes with new nodes inserted after the specified node.
        /// </summary>
        /// <typeparam name="TRoot">The type of the root node.</typeparam>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="nodeInList">The node to insert after; a descendant of the root node an element of a list member.</param>
        /// <param name="newNodes">A sequence of nodes to insert into the tree immediately after the specified node.</param>
        public static TRoot InsertNodesAfter<TRoot>(this TRoot root, SyntaxNode nodeInList, IEnumerable<SyntaxNode> newNodes)
            where TRoot : SyntaxNode
        {
            return (TRoot)root.InsertNodesInListCore(nodeInList, newNodes, insertBefore: false);
        }

        public static string GetContent<TNode>(this TNode node) where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var tokens = node.DescendantNodes().Where(n => n.IsToken).Cast<SyntaxToken>();
            var content = string.Concat(tokens.Select(t => t.Content));
            return content;
        }

        public static string GetTagName(this MarkupTagBlockSyntax tagBlock)
        {
            if (tagBlock == null)
            {
                throw new ArgumentNullException(nameof(tagBlock));
            }

            var child = tagBlock.Children[0];

            if (tagBlock.Children.Count == 0 || !(child is MarkupTextLiteralSyntax))
            {
                return null;
            }

            var childLiteral = (MarkupTextLiteralSyntax)child;
            SyntaxToken textToken = null;
            for (var i = 0; i < childLiteral.LiteralTokens.Count; i++)
            {
                var token = childLiteral.LiteralTokens[i];

                if (token != null &&
                    (token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.Text))
                {
                    textToken = token;
                    break;
                }
            }

            if (textToken == null)
            {
                return null;
            }

            return textToken.Kind == SyntaxKind.Whitespace ? null : textToken.Content;
        }

        public static string GetTagName(this MarkupTagHelperStartTagSyntax tagBlock)
        {
            if (tagBlock == null)
            {
                throw new ArgumentNullException(nameof(tagBlock));
            }

            var child = tagBlock.Children[0];

            if (tagBlock.Children.Count == 0 || !(child is MarkupTextLiteralSyntax))
            {
                return null;
            }

            var childLiteral = (MarkupTextLiteralSyntax)child;
            SyntaxToken textToken = null;
            for (var i = 0; i < childLiteral.LiteralTokens.Count; i++)
            {
                var token = childLiteral.LiteralTokens[i];

                if (token != null &&
                    (token.Kind == SyntaxKind.Whitespace || token.Kind == SyntaxKind.Text))
                {
                    textToken = token;
                    break;
                }
            }

            if (textToken == null)
            {
                return null;
            }

            return textToken.Kind == SyntaxKind.Whitespace ? null : textToken.Content;
        }

        public static bool IsSelfClosing(this MarkupTagBlockSyntax tagBlock)
        {
            if (tagBlock == null)
            {
                throw new ArgumentNullException(nameof(tagBlock));
            }

            var lastChild = tagBlock.ChildNodes().LastOrDefault();

            return lastChild?.GetContent().EndsWith("/>", StringComparison.Ordinal) ?? false;
        }

        public static bool IsVoidElement(this MarkupTagBlockSyntax tagBlock)
        {
            if (tagBlock == null)
            {
                throw new ArgumentNullException(nameof(tagBlock));
            }

            return VoidElements.Contains(tagBlock.GetTagName());
        }
    }
}
