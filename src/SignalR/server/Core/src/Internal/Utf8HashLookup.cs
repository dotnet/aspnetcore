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
    private int[] buckets;
    private Slot[] slots;
    private int count;

    private const int HashCodeMask = 0x7fffffff;

    internal Utf8HashLookup()
    {
        buckets = new int[7];
        slots = new Slot[7];
    }

    internal void Add(string value)
    {
        var hashCode = GetKeyHashCode(value.AsSpan());

        if (count == slots.Length)
        {
            Resize();
        }

        int index = count;
        count++;

        int bucket = hashCode % buckets.Length;
        slots[index].hashCode = hashCode;
        slots[index].key = value;
        slots[index].value = value;
        slots[index].next = buckets[bucket] - 1;
        buckets[bucket] = index + 1;
    }

    internal bool TryGetValue(ReadOnlySpan<byte> utf8, [MaybeNullWhen(false), AllowNull] out string value)
    {
        const int StackAllocThreshold = 128;

        // Transcode to utf16 for comparison
        char[]? pooledName = null;
        var count = Encoding.UTF8.GetCharCount(utf8);
        var chars = count <= StackAllocThreshold ?
            stackalloc char[StackAllocThreshold] :
            (pooledName = ArrayPool<char>.Shared.Rent(count));
        var encoded = Encoding.UTF8.GetChars(utf8, chars);
        var hasValue = TryGetValue(chars[..encoded], out value);
        if (pooledName is not null)
        {
            ArrayPool<char>.Shared.Return(pooledName);
        }

        return hasValue;
    }

    private bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false), AllowNull] out string value)
    {
        var hashCode = GetKeyHashCode(key);

        for (var i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
        {
            if (slots[i].hashCode == hashCode && key.Equals(slots[i].key, StringComparison.OrdinalIgnoreCase))
            {
                value = slots[i].value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static int GetKeyHashCode(ReadOnlySpan<char> key)
    {
        return HashCodeMask & string.GetHashCode(key);
    }

    private void Resize()
    {
        var newSize = checked(count * 2 + 1);
        var newBuckets = new int[newSize];
        var newSlots = new Slot[newSize];
        Array.Copy(slots, newSlots, count);
        for (int i = 0; i < count; i++)
        {
            int bucket = newSlots[i].hashCode % newSize;
            newSlots[i].next = newBuckets[bucket] - 1;
            newBuckets[bucket] = i + 1;
        }
        buckets = newBuckets;
        slots = newSlots;
    }

    internal struct Slot
    {
        internal int hashCode;
        internal int next;
        internal string key;
        internal string value;
    }
}
