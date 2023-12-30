// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal readonly struct PrefixResolver : IDisposable
{
    private readonly FormKey[] _sortedKeys;
    private readonly int _length;

    public bool HasValues => _length > 0 && _sortedKeys != null;

    public PrefixResolver(IEnumerable<FormKey> readOnlyMemoryKeys, int count)
    {
        _sortedKeys = ArrayPool<FormKey>.Shared.Rent(count);
        _length = count;
        var i = 0;
        foreach (var key in readOnlyMemoryKeys)
        {
            _sortedKeys[i++] = key;
        }

        Array.Sort(_sortedKeys, 0, count, FormKeyComparer.SortCriteria);
    }

    internal bool HasPrefix(ReadOnlyMemory<char> currentPrefixBuffer)
    {
        if (currentPrefixBuffer.Length == 0)
        {
            return _length > 0 && !(_length == 1 && _sortedKeys[0].Value.Length == 0);
        }
        return Array.BinarySearch(_sortedKeys, 0, _length, new FormKey(currentPrefixBuffer), FormKeyComparer.PrefixCriteria) >= 0;
    }

    public void Dispose()
    {
        if (_sortedKeys != null)
        {
            ArrayPool<FormKey>.Shared.Return(_sortedKeys);
        }
    }

    private class FormKeyComparer(bool checkPrefix) : IComparer<FormKey>
    {
        internal static readonly FormKeyComparer SortCriteria = new(checkPrefix: false);
        internal static readonly FormKeyComparer PrefixCriteria = new(checkPrefix: true);

        // When comparing values, y is the element we are trying to find.
        public int Compare(FormKey x, FormKey y)
        {
            var separatorX = 0;
            var separatorY = 0;
            var currentXPos = 0;
            var currentYPos = 0;
            while (separatorX != -1 && separatorY != -1)
            {
                separatorX = x.Value.Span[currentXPos..].IndexOfAny('.', '[');
                separatorY = y.Value.Span[currentYPos..].IndexOfAny('.', '[');

                if (separatorX == -1 && separatorY == -1)
                {
                    // no more segments, compare the remaining substrings
                    return MemoryExtensions.CompareTo(x.Value.Span[currentXPos..], y.Value.Span[currentYPos..], StringComparison.Ordinal);
                }
                else if (separatorX == -1)
                {
                    // x has no more segments, but y does, so x is less than y
                    return -1;
                }
                else if (separatorY == -1)
                {
                    if (!checkPrefix)
                    {
                        // We are just sorting, so x is greater than y because it has more segments.
                        return 1;
                    }

                    var match = MemoryExtensions.CompareTo(
                        x.Value.Span[currentXPos..][..separatorX],
                        y.Value.Span[currentYPos..], StringComparison.Ordinal);

                    return match;
                }

                // both have segments, compare the segments
                var segmentX = x.Value.Span[currentXPos..][..separatorX];
                var segmentY = y.Value.Span[currentYPos..][..separatorY];
                var compareResult = MemoryExtensions.CompareTo(segmentX, segmentY, StringComparison.Ordinal);
                if (compareResult != 0)
                {
                    return compareResult;
                }

                currentXPos += separatorX + 1;
                currentYPos += separatorY + 1;
            }

            return 0;
        }
    }
}
