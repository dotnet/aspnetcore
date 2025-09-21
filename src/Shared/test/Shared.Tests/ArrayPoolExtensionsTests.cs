// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers;

public sealed class ArrayPoolExtensionsTests
{
    [Fact]
    public void Return_PartiallyClearsArray_WithSpecifiedLengthToClear()
    {
        ArrayPool<int> pool = ArrayPool<int>.Create();
        int[] array = pool.Rent(64);

        array.AsSpan().Fill(int.MaxValue);

        pool.Return(array, 42);

        Assert.True(array.AsSpan(0, 42).IndexOfAnyExcept(0) < 0);
        Assert.True(array.AsSpan(42).IndexOfAnyExcept(int.MaxValue) < 0);
    }
}
