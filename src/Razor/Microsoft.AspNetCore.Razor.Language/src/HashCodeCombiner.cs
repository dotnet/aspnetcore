// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Internal;

internal struct HashCodeCombiner
{
    private long _combinedHash64;

    public int CombinedHash
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return _combinedHash64.GetHashCode(); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HashCodeCombiner(long seed)
    {
        _combinedHash64 = seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(HashCodeCombiner self)
    {
        return self.CombinedHash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int i)
    {
        _combinedHash64 = ((_combinedHash64 << 5) + _combinedHash64) ^ i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(T? o)
    {
        Add(o?.GetHashCode() ?? 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<TValue>(TValue value, IEqualityComparer<TValue> comparer)
    {
        var hashCode = value != null ? comparer.GetHashCode(value) : 0;
        Add(hashCode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashCodeCombiner Start()
    {
        return new HashCodeCombiner(0x1505L);
    }
}
