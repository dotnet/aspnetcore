// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

/// <summary>
/// Root of the embedded language syntax hierarchy.  EmbeddedSyntaxNodes are very similar to 
/// Roslyn Red-Nodes in concept, though there are differences for ease of implementation.
/// </summary>
internal abstract class EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
    where TSyntaxKind : struct
    where TSyntaxNode : EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
{
    public readonly TSyntaxKind Kind;
    private TextSpan? _fullSpan;

    protected EmbeddedSyntaxNode(TSyntaxKind kind)
    {
        Debug.Assert((int)(object)kind != 0);
        Kind = kind;
    }

    internal abstract int ChildCount { get; }
    internal abstract EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode> ChildAt(int index);

    public EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode> this[int index] => ChildAt(index);

    public TextSpan GetSpan()
    {
        var start = int.MaxValue;
        var end = 0;

        GetSpan(ref start, ref end);

        return TextSpan.FromBounds(start, end);
    }

    public TextSpan? GetFullSpan()
        => _fullSpan ??= ComputeFullSpan();

    private TextSpan? ComputeFullSpan()
    {
        var start = ComputeStart();
        var end = ComputeEnd();
        if (start == null || end == null)
        {
            return null;
        }

        return TextSpan.FromBounds(start.Value, end.Value);

        int? ComputeStart()
        {
            for (int i = 0, n = ChildCount; i < n; i++)
            {
                var child = ChildAt(i);
                var span = child.GetFullSpan();
                if (span != null)
                {
                    return span.Value.Start;
                }
            }

            return null;
        }

        int? ComputeEnd()
        {
            for (var i = ChildCount - 1; i >= 0; i--)
            {
                var child = ChildAt(i);
                var span = child.GetFullSpan();
                if (span != null)
                {
                    return span.Value.End;
                }
            }

            return null;
        }
    }

    private void GetSpan(ref int start, ref int end)
    {
        foreach (var child in this)
        {
            if (child.IsNode)
            {
                child.Node.GetSpan(ref start, ref end);
            }
            else
            {
                var token = child.Token;
                if (!token.IsMissing)
                {
                    start = Math.Min(token.VirtualChars[0].Span.Start, start);
                    end = Math.Max(token.VirtualChars.Last().Span.End, end);
                }
            }
        }
    }

    public bool Contains(AspNetCoreVirtualChar virtualChar)
    {
        foreach (var child in this)
        {
            if (child.IsNode)
            {
                if (child.Node.Contains(virtualChar))
                {
                    return true;
                }
            }
            else
            {
                if (child.Token.VirtualChars.Contains(virtualChar))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the string representation of this node, not including its leading and trailing trivia.
    /// </summary>
    /// <returns>The string representation of this node, not including its leading and trailing trivia.</returns>
    /// <remarks>The length of the returned string is always the same as Span.Length</remarks>
    public override string ToString()
    {
        var sb = new StringBuilder();
        WriteTo(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Returns full string representation of this node including its leading and trailing trivia.
    /// </summary>
    /// <returns>The full string representation of this node including its leading and trailing trivia.</returns>
    /// <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
    public string ToFullString()
    {
        var sb = new StringBuilder();
        WriteTo(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Writes the node to a stringbuilder.
    /// </summary>
    /// <param name="leading">If false, leading trivia will not be added</param>
    /// <param name="trailing">If false, trailing trivia will not be added</param>
    public void WriteTo(StringBuilder sb)
    {
        for (var i = 0; i < ChildCount; i++)
        {
            var child = this[i];

            if (child.IsNode)
            {
                child.Node.WriteTo(sb);
            }
            else
            {
                child.Token.WriteTo(sb);
            }
        }
    }

    public Enumerator GetEnumerator()
        => new(this);

    public struct Enumerator
    {
        private readonly EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode> _node;
        private readonly int _childCount;
        private int _currentIndex;

        public Enumerator(EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode> node)
        {
            _node = node;
            _childCount = _node.ChildCount;
            _currentIndex = -1;
            Current = default;
        }

        public EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode> Current { get; private set; }

        public bool MoveNext()
        {
            _currentIndex++;
            if (_currentIndex >= _childCount)
            {
                Current = default;
                return false;
            }

            Current = _node.ChildAt(_currentIndex);
            return true;
        }
    }
}
