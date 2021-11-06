// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal class SyntaxTriviaListBuilder
{
    private SyntaxTrivia[] _nodes;

    public SyntaxTriviaListBuilder(int size)
    {
        _nodes = new SyntaxTrivia[size];
    }

    public static SyntaxTriviaListBuilder Create()
    {
        return new SyntaxTriviaListBuilder(4);
    }

    public static SyntaxTriviaList Create(IEnumerable<SyntaxTrivia> trivia)
    {
        if (trivia == null)
        {
            return new SyntaxTriviaList();
        }

        var builder = Create();
        builder.AddRange(trivia);
        return builder.ToList();
    }

    public int Count { get; private set; }

    public void Clear()
    {
        Count = 0;
    }

    public SyntaxTrivia this[int index]
    {
        get
        {
            if (index < 0 || index > Count)
            {
                throw new IndexOutOfRangeException();
            }

            return _nodes[index];
        }
    }

    public void AddRange(IEnumerable<SyntaxTrivia> items)
    {
        if (items != null)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
    }

    public SyntaxTriviaListBuilder Add(SyntaxTrivia item)
    {
        if (_nodes == null || Count >= _nodes.Length)
        {
            Grow(Count == 0 ? 8 : _nodes.Length * 2);
        }

        _nodes[Count++] = item;
        return this;
    }

    public void Add(SyntaxTrivia[] items)
    {
        Add(items, 0, items.Length);
    }

    public void Add(SyntaxTrivia[] items, int offset, int length)
    {
        if (_nodes == null || Count + length > _nodes.Length)
        {
            Grow(Count + length);
        }

        Array.Copy(items, offset, _nodes, Count, length);
        Count += length;
    }

    public void Add(in SyntaxTriviaList list)
    {
        Add(list, 0, list.Count);
    }

    public void Add(in SyntaxTriviaList list, int offset, int length)
    {
        if (_nodes == null || Count + length > _nodes.Length)
        {
            Grow(Count + length);
        }

        list.CopyTo(offset, _nodes, Count, length);
        Count += length;
    }

    private void Grow(int size)
    {
        var tmp = new SyntaxTrivia[size];
        Array.Copy(_nodes, tmp, _nodes.Length);
        _nodes = tmp;
    }

    public static implicit operator SyntaxTriviaList(SyntaxTriviaListBuilder builder)
    {
        return builder.ToList();
    }

    public SyntaxTriviaList ToList()
    {
        if (Count > 0)
        {
            var tmp = new ArrayElement<GreenNode>[Count];
            for (var i = 0; i < Count; i++)
            {
                tmp[i].Value = _nodes[i].Green;
            }
            return new SyntaxTriviaList(InternalSyntax.SyntaxList.List(tmp).CreateRed(), position: 0, index: 0);
        }
        else
        {
            return default(SyntaxTriviaList);
        }
    }
}
