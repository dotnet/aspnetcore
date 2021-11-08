// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxNodeExtensions
{
    public static TNode WithAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : SyntaxNode
    {
        return (TNode)node.Green.SetAnnotations(annotations).CreateRed(node.Parent, node.Position);
    }

    public static object GetAnnotationValue<TNode>(this TNode node, string key) where TNode : SyntaxNode
    {
        if (!node.ContainsAnnotations)
        {
            return null;
        }

        var annotations = node.GetAnnotations();
        foreach (var annotation in annotations)
        {
            if (annotation.Kind == key)
            {
                return annotation.Data;
            }
        }

        return null;
    }

    public static TNode WithDiagnostics<TNode>(this TNode node, params RazorDiagnostic[] diagnostics) where TNode : SyntaxNode
    {
        return (TNode)node.Green.SetDiagnostics(diagnostics).CreateRed(node.Parent, node.Position);
    }

    public static TNode AppendDiagnostic<TNode>(this TNode node, params RazorDiagnostic[] diagnostics) where TNode : SyntaxNode
    {
        var existingDiagnostics = node.GetDiagnostics();
        var allDiagnostics = new RazorDiagnostic[diagnostics.Length + existingDiagnostics.Length];
        Array.Copy(existingDiagnostics, allDiagnostics, existingDiagnostics.Length);
        Array.Copy(diagnostics, 0, allDiagnostics, existingDiagnostics.Length, diagnostics.Length);

        return (TNode)node.WithDiagnostics(allDiagnostics);
    }

    /// <summary>
    /// Gets top-level and nested diagnostics from the <paramref name="node"/>.
    /// </summary>
    /// <typeparam name="TNode">The type of syntax node.</typeparam>
    /// <param name="node">The syntax node.</param>
    /// <returns>The list of <see cref="RazorDiagnostic"/>s.</returns>
    public static IReadOnlyList<RazorDiagnostic> GetAllDiagnostics<TNode>(this TNode node) where TNode : SyntaxNode
    {
        var walker = new DiagnosticSyntaxWalker();
        walker.Visit(node);

        return walker.Diagnostics;
    }

    public static SourceLocation GetSourceLocation(this SyntaxNode node, RazorSourceDocument source)
    {
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
        var location = node.GetSourceLocation(source);
        var endLocation = source.Lines.GetLocation(node.EndPosition);
        var lineCount = endLocation.LineIndex - location.LineIndex;
        return new SourceSpan(location.FilePath, location.AbsoluteIndex, location.LineIndex, location.CharacterIndex, node.FullWidth, lineCount, endLocation.CharacterIndex);
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
        var builder = new StringBuilder();
        foreach (var token in node.DescendantNodesAndSelf())
        {
            if (!token.IsToken)
            {
                continue;
            }

            var syntaxToken = (SyntaxToken)token;
            builder.Append(syntaxToken.Content);
        }

        return builder.ToString();
    }

    private class DiagnosticSyntaxWalker : SyntaxWalker
    {
        private readonly List<RazorDiagnostic> _diagnostics;

        public DiagnosticSyntaxWalker()
        {
            _diagnostics = new List<RazorDiagnostic>();
        }

        public IReadOnlyList<RazorDiagnostic> Diagnostics => _diagnostics;

        public override void Visit(SyntaxNode node)
        {
            if (node?.ContainsDiagnostics == true)
            {
                var diagnostics = node.GetDiagnostics();

                _diagnostics.AddRange(diagnostics);

                base.Visit(node);
            }
        }
    }
}
