// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

internal sealed class ArrayPoolBufferSource : ICharBufferSource
{
    private readonly ArrayPool<char> _pool;

    public ArrayPoolBufferSource(ArrayPool<char> pool)
    {
        _pool = pool;
    }

    public char[] Rent(int bufferSize) => _pool.Rent(bufferSize);

    public void Return(char[] buffer) => _pool.Return(buffer);
}
