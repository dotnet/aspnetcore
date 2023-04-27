// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies the mode to use when serializing component persistent state.
/// </summary>
public enum PersistedStateSerializationMode
{
    /// <summary>
    /// Indicates that the serialization mode should be inferred from the current request context.
    /// </summary>
    Infer = 1,

    /// <summary>
    /// Indicates that the state should be persisted so that execution may resume on Server.
    /// </summary>
    Server = 2,

    /// <summary>
    /// Indicates that the state should be persisted so that execution may resume on WebAssembly.
    /// </summary>
    WebAssembly = 3,
}
