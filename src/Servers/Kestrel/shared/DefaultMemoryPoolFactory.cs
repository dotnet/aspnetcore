// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal;

internal sealed class DefaultMemoryPoolFactory : IMemoryPoolFactory
{
    public static DefaultMemoryPoolFactory Instance { get; } = new DefaultMemoryPoolFactory();

    public MemoryPool<byte> CreatePool()
    {
        return MemoryPool<byte>.Shared;
    }
}
