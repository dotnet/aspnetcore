// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal class DefaultRazorIntermediateNodeBuilder : IntermediateNodeBuilder
{
    private readonly List<IntermediateNode> _stack;
    private int _depth;

    public DefaultRazorIntermediateNodeBuilder()
    {
        _stack = new List<IntermediateNode>();
    }

    public override IntermediateNode Current
    {
        get
        {
            return _depth > 0 ? _stack[_depth - 1] : null;
        }
    }

    public override void Add(IntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        Current.Children.Add(node);
    }

    public override void Insert(int index, IntermediateNode node)
    {
        if (index < 0 || index - Current.Children.Count > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index == Current.Children.Count)
        {
            // Allow inserting at 'Children.Count' to be friendlier than List<> typically is.
            Current.Children.Add(node);
        }
        else
        {
            Current.Children.Insert(index, node);
        }
    }

    public override IntermediateNode Build()
    {
        IntermediateNode node = null;
        while (_depth > 0)
        {
            node = Pop();
        }

        return node;
    }

    public override IntermediateNode Pop()
    {
        if (_depth == 0)
        {
            throw new InvalidOperationException(Resources.FormatIntermediateNodeBuilder_PopInvalid(nameof(Pop)));
        }

        var node = _stack[--_depth];
        return node;
    }

    public override void Push(IntermediateNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (_depth >= _stack.Count)
        {
            _stack.Add(node);
        }
        else
        {
            _stack[_depth] = node;
        }

        if (_depth > 0)
        {
            var parent = _stack[_depth - 1];
            parent.Children.Add(node);
        }

        _depth++;
    }
}
