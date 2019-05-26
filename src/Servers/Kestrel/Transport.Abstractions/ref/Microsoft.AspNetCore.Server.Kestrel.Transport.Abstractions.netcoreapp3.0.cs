// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions
{
    public partial class FileHandleEndPoint : System.Net.EndPoint
    {
        public FileHandleEndPoint(ulong fileHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.FileHandleType fileHandleType) { }
        public ulong FileHandle { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.FileHandleType FileHandleType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public enum FileHandleType
    {
        Auto = 0,
        Tcp = 1,
        Pipe = 2,
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public static partial class KestrelMemoryPool
    {
        public static readonly int MinimumSegmentSize;
        public static System.Buffers.MemoryPool<byte> Create() { throw null; }
        public static System.Buffers.MemoryPool<byte> CreateSlabMemoryPool() { throw null; }
    }
}
