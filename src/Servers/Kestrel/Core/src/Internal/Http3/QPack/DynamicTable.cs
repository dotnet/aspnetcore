// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http.QPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
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
        public void Insert(Span<byte> name, Span<byte> value)
        {
        }

        // TODO 
        public void Resize(int maxSize)
        {
        }

        // TODO 
        internal void Duplicate(int index)
        {
            throw new NotImplementedException();
        }
    }
}
