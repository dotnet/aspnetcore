// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal class SyntaxListBuilder
{
    private ArrayElement<GreenNode>[] _nodes;

    public int Count { get; private set; }

    public SyntaxListBuilder(int size)
    {
        _nodes = new ArrayElement<GreenNode>[size];
    }

    public static SyntaxListBuilder Create()
    {
        return new SyntaxListBuilder(8);
    }

    public void Clear()
    {
        Count = 0;
    }

    public GreenNode this[int index]
    {
        get
        {
            return _nodes[index];
        }

        set
        {
            _nodes[index].Value = value;
        }
    }

    public void Add(GreenNode item)
    {
        if (item == null) return;

        if (item.IsList)
        {
            var slotCount = item.SlotCount;

            // Necessary, but not sufficient (e.g. for nested lists).
            EnsureAdditionalCapacity(slotCount);

            for (var i = 0; i < slotCount; i++)
            {
                Add(item.GetSlot(i));
            }
        }
        else
        {
            EnsureAdditionalCapacity(1);

            _nodes[Count++].Value = item;
        }
    }

    public void AddRange(GreenNode[] items)
    {
        AddRange(items, 0, items.Length);
    }

    public void AddRange(GreenNode[] items, int offset, int length)
    {
        // Necessary, but not sufficient (e.g. for nested lists).
        EnsureAdditionalCapacity(length - offset);

        var oldCount = Count;

        for (var i = offset; i < length; i++)
        {
            Add(items[i]);
        }

        Validate(oldCount, Count);
    }

    [Conditional("DEBUG")]
    private void Validate(int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            Debug.Assert(_nodes[i].Value != null);
        }
    }

    public void AddRange(SyntaxList<GreenNode> list)
    {
        this.AddRange(list, 0, list.Count);
    }

    public void AddRange(SyntaxList<GreenNode> list, int offset, int length)
    {
        // Necessary, but not sufficient (e.g. for nested lists).
        EnsureAdditionalCapacity(length - offset);

        var oldCount = Count;

        for (var i = offset; i < length; i++)
        {
            Add(list[i]);
        }

        Validate(oldCount, Count);
    }

    public void AddRange<TNode>(SyntaxList<TNode> list) where TNode : GreenNode
    {
        this.AddRange(list, 0, list.Count);
    }

    public void AddRange<TNode>(SyntaxList<TNode> list, int offset, int length) where TNode : GreenNode
    {
        AddRange(new SyntaxList<GreenNode>(list.Node), offset, length);
    }

    public void RemoveLast()
    {
        Count--;
        _nodes[Count].Value = null;
    }

    private void EnsureAdditionalCapacity(int additionalCount)
    {
        var currentSize = _nodes.Length;
        var requiredSize = Count + additionalCount;

        if (requiredSize <= currentSize) return;

        var newSize =
            requiredSize < 8 ? 8 :
            requiredSize >= (int.MaxValue / 2) ? int.MaxValue :
            Math.Max(requiredSize, currentSize * 2); // NB: Size will *at least* double.
        Debug.Assert(newSize >= requiredSize);

        Array.Resize(ref _nodes, newSize);
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

    public GreenNode[] ToArray()
    {
        var array = new GreenNode[Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _nodes[i];
        }

        return array;
    }

    internal GreenNode ToListNode()
    {
        switch (Count)
        {
            case 0:
                return null;
            case 1:
                return _nodes[0];
            case 2:
                return SyntaxList.List(_nodes[0], _nodes[1]);
            case 3:
                return SyntaxList.List(_nodes[0], _nodes[1], _nodes[2]);
            default:
                var tmp = new ArrayElement<GreenNode>[Count];
                Array.Copy(_nodes, tmp, Count);
                return SyntaxList.List(tmp);
        }
    }

    public SyntaxList<GreenNode> ToList()
    {
        return new SyntaxList<GreenNode>(ToListNode());
    }

    public SyntaxList<TNode> ToList<TNode>() where TNode : GreenNode
    {
        return new SyntaxList<TNode>(ToListNode());
    }
}
