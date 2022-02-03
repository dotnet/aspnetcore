// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.QPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack;

//   The size of the dynamic table is the sum of the size of its entries.
//   The size of an entry is the sum of its name's length in bytes (as
//   defined in Section 4.1.2), its value's length in bytes, and 32.

internal class DynamicTable
{
    // The encoder sends a Set Dynamic Table Capacity
    // instruction(Section 4.3.1) with a non-zero capacity to begin using
    // the dynamic table.
    public DynamicTable(int maxSize)
    {
    }

    public HeaderField this[int index]
    {
        get
        {
            return new HeaderField();
        }
    }

    // TODO
    public static void Insert(Span<byte> name, Span<byte> value)
    {
    }

    // TODO
    public static void Resize(int maxSize)
    {
    }

    // TODO
    internal void Duplicate(int index)
    {
        throw new NotImplementedException();
    }
}
