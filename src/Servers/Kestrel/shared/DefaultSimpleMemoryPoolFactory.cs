// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal;

internal sealed class DefaultSimpleMemoryPoolFactory : IMemoryPoolFactory<byte>
{
    public static DefaultSimpleMemoryPoolFactory Instance { get; } = new DefaultSimpleMemoryPoolFactory();

    public MemoryPool<byte> Create()
    {
        return MemoryPool<byte>.Shared;
    }
}
