// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class SyntaxRewriter : SyntaxVisitor<SyntaxNode>
{
    private int _recursionDepth;

    public override SyntaxNode Visit(SyntaxNode node)
    {
        if (node != null)
        {
            _recursionDepth++;
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

            var result = node.Accept(this);

            _recursionDepth--;
            return result;
        }
        else
        {
            return null;
        }
    }

    public override SyntaxNode VisitToken(SyntaxToken token)
    {
        // PERF: This is a hot method, so it has been written to minimize the following:
        // 1. Virtual method calls
        // 2. Copying of structs
        // 3. Repeated null checks

        // PERF: Avoid testing node for null more than once
        var node = token?.Green;
        if (node == null)
        {
            return token;
        }

        // PERF: Make one virtual method call each to get the leading and trailing trivia
        var leadingTrivia = node.GetLeadingTrivia();
        var trailingTrivia = node.GetTrailingTrivia();

        // Trivia is either null or a non-empty list (there's no such thing as an empty green list)
        Debug.Assert(leadingTrivia == null || !leadingTrivia.IsList || leadingTrivia.SlotCount > 0);
        Debug.Assert(trailingTrivia == null || !trailingTrivia.IsList || trailingTrivia.SlotCount > 0);

        if (leadingTrivia != null)
        {
            // PERF: Expand token.LeadingTrivia when node is not null.
            var leading = VisitList(new SyntaxTriviaList(leadingTrivia.CreateRed(token, token.Position)));

            if (trailingTrivia != null)
            {
                // Both leading and trailing trivia

                // PERF: Expand token.TrailingTrivia when node is not null and leadingTrivia is not null.
                // Also avoid node.Width because it makes a virtual call to GetText. Instead use node.FullWidth - trailingTrivia.FullWidth.
                var index = leadingTrivia.IsList ? leadingTrivia.SlotCount : 1;
                var position = token.Position + node.FullWidth - trailingTrivia.FullWidth;
                var trailing = VisitList(new SyntaxTriviaList(trailingTrivia.CreateRed(token, position), position, index));

                if (leading.Node.Green != leadingTrivia)
                {
                    token = token.WithLeadingTrivia(leading);
                }

                return trailing.Node.Green != trailingTrivia ? token.WithTrailingTrivia(trailing) : token;
            }
            else
            {
                // Leading trivia only
                return leading.Node.Green != leadingTrivia ? token.WithLeadingTrivia(leading) : token;
            }
        }
        else if (trailingTrivia != null)
        {
            // Trailing trivia only
            // PERF: Expand token.TrailingTrivia when node is not null and leading is null.
            // Also avoid node.Width because it makes a virtual call to GetText. Instead use node.FullWidth - trailingTrivia.FullWidth.
            var position = token.Position + node.FullWidth - trailingTrivia.FullWidth;
            var trailing = VisitList(new SyntaxTriviaList(trailingTrivia.CreateRed(token, position), position, index: 0));
            return trailing.Node.Green != trailingTrivia ? token.WithTrailingTrivia(trailing) : token;
        }
        else
        {
            // No trivia
            return token;
        }
    }

    public virtual SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
    {
        SyntaxListBuilder alternate = null;
        for (int i = 0, n = list.Count; i < n; i++)
        {
            var item = list[i];
            var visited = VisitListElement(item);
            if (item != visited && alternate == null)
            {
                alternate = new SyntaxListBuilder(n);
                alternate.AddRange(list, 0, i);
            }

            if (alternate != null && visited != null)
            {
                alternate.Add(visited);
            }
        }

        if (alternate != null)
        {
            return alternate.ToList();
        }

        return list;
    }

    public override SyntaxNode VisitTrivia(SyntaxTrivia trivia)
    {
        return trivia;
    }

    public virtual SyntaxTriviaList VisitList(SyntaxTriviaList list)
    {
        var count = list.Count;
        if (count != 0)
        {
            SyntaxTriviaListBuilder alternate = null;
            var index = -1;

            foreach (var item in list)
            {
                index++;
                var visited = VisitListElement(item);

                //skip the null check since SyntaxTrivia is a value type
                if (visited != item && alternate == null)
                {
                    alternate = new SyntaxTriviaListBuilder(count);
                    alternate.Add(list, 0, index);
                }

                if (alternate != null && visited != null)
                {
                    alternate.Add(visited);
                }
            }

            if (alternate != null)
            {
                return alternate.ToList();
            }
        }

        return list;
    }

    public virtual TNode VisitListElement<TNode>(TNode node) where TNode : SyntaxNode
    {
        return (TNode)(SyntaxNode)Visit(node);
    }
}
