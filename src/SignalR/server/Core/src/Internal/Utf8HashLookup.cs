// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Internal;

/// <summary>
/// A small dictionary optimized for utf8 string lookup via spans. Adapted from https://github.com/dotnet/runtime/blob/4ed596ef63e60ce54cfb41d55928f0fe45f65cf3/src/libraries/System.Linq.Parallel/src/System/Linq/Parallel/Utils/HashLookup.cs.
/// </summary>
internal sealed class Utf8HashLookup
{
    private int[] _buckets;
    private int[] _caseSensitiveBuckets;
    private Slot[] _slots;
    private int _count;

    private const int HashCodeMask = 0x7fffffff;

    internal Utf8HashLookup()
    {
        _buckets = new int[7];
        _caseSensitiveBuckets = new int[7];
        _slots = new Slot[7];
    }

    internal void Add(string value)
    {
        if (_count == _slots.Length)
        {
            Resize();
        }

        int slotIndex = _count;
        _count++;

        var encodedValue = Encoding.UTF8.GetBytes(value);
        var hashCode = GetHashCode(value.AsSpan());
        var caseSensitiveHashCode = GetCaseSensitiveHashCode(encodedValue);
        int bucketIndex = hashCode % _buckets.Length;
        int caseSensitiveBucketIndex = caseSensitiveHashCode % _caseSensitiveBuckets.Length;

        _slots[slotIndex].hashCode = hashCode;
        _slots[slotIndex].caseSensitiveHashCode = caseSensitiveHashCode;

        _slots[slotIndex].value = value;
        _slots[slotIndex].encodedValue = encodedValue;

        _slots[slotIndex].next = _buckets[bucketIndex] - 1;
        _slots[slotIndex].caseSensitiveNext = _caseSensitiveBuckets[caseSensitiveBucketIndex] - 1;

        _buckets[bucketIndex] = slotIndex + 1;
        _caseSensitiveBuckets[caseSensitiveBucketIndex] = slotIndex + 1;
    }

    internal bool TryGetValue(ReadOnlySpan<byte> encodedValue, [MaybeNullWhen(false), AllowNull] out string value)
    {
        var caseSensitiveHashCode = GetCaseSensitiveHashCode(encodedValue);

        for (var i = _caseSensitiveBuckets[caseSensitiveHashCode % _caseSensitiveBuckets.Length] - 1; i >= 0; i = _slots[i].caseSensitiveNext)
        {
            if (_slots[i].caseSensitiveHashCode == caseSensitiveHashCode && encodedValue.SequenceEqual(_slots[i].encodedValue.AsSpan()))
            {
                value = _slots[i].value;
                return true;
            }
        }

        // If we cannot find a case-sensitive match, we transcode the encodedValue to a stackalloced UTF16 string
        // and do an OrdinalIgnoreCase comparison.
        return TryGetValueSlow(encodedValue, out value);
    }

    private bool TryGetValueSlow(ReadOnlySpan<byte> encodedValue, [MaybeNullWhen(false), AllowNull] out string value)
    {
        const int StackAllocThreshold = 128;

        char[]? pooled = null;
        var count = Encoding.UTF8.GetCharCount(encodedValue);
        var chars = count <= StackAllocThreshold ?
            stackalloc char[StackAllocThreshold] :
            (pooled = ArrayPool<char>.Shared.Rent(count));
        var encoded = Encoding.UTF8.GetChars(encodedValue, chars);
        var hasValue = TryGetValueFromChars(chars[..encoded], out value);
        if (pooled is not null)
        {
            ArrayPool<char>.Shared.Return(pooled);
        }

        return hasValue;
    }

    private bool TryGetValueFromChars(ReadOnlySpan<char> key, [MaybeNullWhen(false), AllowNull] out string value)
    {
        var hashCode = GetHashCode(key);

        for (var i = _buckets[hashCode % _buckets.Length] - 1; i >= 0; i = _slots[i].next)
        {
            if (_slots[i].hashCode == hashCode && key.Equals(_slots[i].value, StringComparison.OrdinalIgnoreCase))
            {
                value = _slots[i].value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static int GetHashCode(ReadOnlySpan<char> value) =>
        HashCodeMask & string.GetHashCode(value, StringComparison.OrdinalIgnoreCase);

    private static int GetCaseSensitiveHashCode(ReadOnlySpan<byte> encodedValue)
    {
        var hashCode = new HashCode();
        hashCode.AddBytes(encodedValue);
        return HashCodeMask & hashCode.ToHashCode();
    }

    private void Resize()
    {
        var newSize = checked(_count * 2 + 1);
        var newSlots = new Slot[newSize];

        var newBuckets = new int[newSize];
        var newCaseSensitiveBuckets = new int[newSize];

        Array.Copy(_slots, newSlots, _count);

        for (int i = 0; i < _count; i++)
        {
            int bucket = newSlots[i].hashCode % newSize;
            newSlots[i].next = newBuckets[bucket] - 1;
            newBuckets[bucket] = i + 1;

            int caseSensitiveBucket = newSlots[i].caseSensitiveHashCode % newSize;
            newSlots[i].caseSensitiveNext = newCaseSensitiveBuckets[caseSensitiveBucket] - 1;
            newCaseSensitiveBuckets[caseSensitiveBucket] = i + 1;
        }

        _slots = newSlots;

        _buckets = newBuckets;
        _caseSensitiveBuckets = newCaseSensitiveBuckets;
    }

    private struct Slot
    {
        internal int hashCode;
        internal int caseSensitiveHashCode;

        internal string value;
        internal byte[] encodedValue;

        internal int next;
        internal int caseSensitiveNext;
    }
}
