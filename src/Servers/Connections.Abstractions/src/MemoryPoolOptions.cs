// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Options for configuring a memory pool.
/// </summary>
public class MemoryPoolOptions
{
    /// <summary>
    /// Gets or sets the owner of the memory pool. This is used for logging and diagnostics purposes.
    /// </summary>
    public string? Owner { get; set; }
}
