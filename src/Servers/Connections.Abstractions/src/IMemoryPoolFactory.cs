// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// 
/// </summary>
public interface IMemoryPoolFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    MemoryPool<byte> CreatePool();
}
