// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public static class KestrelMemoryPool
    {
        public static MemoryPool<byte> Create() => new SlabMemoryPool();

        public static readonly int MinimumSegmentSize = 4096;
    }
}
