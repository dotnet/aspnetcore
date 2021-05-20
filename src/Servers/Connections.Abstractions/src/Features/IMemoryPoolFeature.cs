// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// The <see cref="MemoryPool{T}"/> used by the connection.
    /// </summary>
    public interface IMemoryPoolFeature
    {
        /// <summary>
        /// Gets the <see cref="MemoryPool{T}"/> used by the connection.
        /// </summary>
        MemoryPool<byte> MemoryPool { get; }
    }
}
