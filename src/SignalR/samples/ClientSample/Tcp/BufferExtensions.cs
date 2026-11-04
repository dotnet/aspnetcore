// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System;

public static class BufferExtensions
{
    public static ArraySegment<byte> GetArray(this Memory<byte> memory)
    {
        return ((ReadOnlyMemory<byte>)memory).GetArray();
    }

    public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory)
    {
        if (!MemoryMarshal.TryGetArray(memory, out var result))
        {
            throw new InvalidOperationException("Buffer backed by array was expected");
        }
        return result;
    }
}
