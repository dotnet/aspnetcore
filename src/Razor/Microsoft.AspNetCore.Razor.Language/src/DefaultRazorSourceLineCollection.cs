// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorSourceLineCollection : RazorSourceLineCollection
{
    private readonly RazorSourceDocument _document;
    private readonly int[] _lineStarts;

    public DefaultRazorSourceLineCollection(RazorSourceDocument document)
    {
        _document = document;
        _lineStarts = GetLineStarts();
    }

    public override int Count => _lineStarts.Length;

    public override int GetLineLength(int index)
    {
        if (index < 0 || index >= _lineStarts.Length)
        {
            throw new IndexOutOfRangeException(nameof(index));
        }

        if (index == _lineStarts.Length - 1)
        {
            // Last line is special.
            return _document.Length - _lineStarts[index];
        }

        return _lineStarts[index + 1] - _lineStarts[index];
    }

    internal override SourceLocation GetLocation(int position)
    {
        if (position < 0 || position > _document.Length)
        {
            throw new IndexOutOfRangeException(nameof(position));
        }

        var index = Array.BinarySearch<int>(_lineStarts, position);
        if (index >= 0)
        {
            // We have an exact match for the start of a line.
            Debug.Assert(_lineStarts[index] == position);

            return new SourceLocation(_document.GetFilePathForDisplay(), position, index, characterIndex: 0);
        }


        // Index is the complement of the line *after* the one we want, because BinarySearch tells
        // us where we'd put position *if* it were the start of a line.
        index = (~index) - 1;
        if (index == -1)
        {
            // There's no preceding line, so it's based on the start of the string
            return new SourceLocation(_document.GetFilePathForDisplay(), position, 0, position);
        }
        else
        {
            var characterIndex = position - _lineStarts[index];
            return new SourceLocation(_document.GetFilePathForDisplay(), position, index, characterIndex);
        }
    }

    private int[] GetLineStarts()
    {
        var starts = new List<int>();

        // We always consider a document to have at least a 0th line, even if it's empty.
        starts.Add(0);

        var length = _document.Length;
        for (var i = 0; i < length; i++)
        {
            var c = _document[i];

            switch (c)
            {
                case '\r':
                    if (i + 1 < length && _document[i + 1] == '\n')
                    {
                        i++;
                    }

                    starts.Add(i + 1);
                    break;

                case '\n':
                    starts.Add(i + 1);
                    break;

                case '\u0085':
                case '\u2028':
                case '\u2029':
                    starts.Add(i + 1);
                    break;
            }
        }

        return starts.ToArray();
    }
}
