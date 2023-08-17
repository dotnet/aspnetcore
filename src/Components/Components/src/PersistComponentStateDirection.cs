// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies the mode to use when serializing component persistent state.
/// </summary>
public enum PersistComponentStateDirection
{
    /// <summary>
    /// Indicates that the state should be persisted on Server.
    /// </summary>
    Server = 1,

    /// <summary>
    /// Indicates that the state should be persisted on WebAssembly.
    /// </summary>
    WebAssembly = 2
}
