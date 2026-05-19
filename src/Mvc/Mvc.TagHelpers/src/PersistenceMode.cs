// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// The way to persist the component application state.
/// </summary>
public enum PersistenceMode
{
    /// <summary>
    /// The state is persisted for a Blazor Server application.
    /// </summary>
    Server,

    /// <summary>
    /// The state is persisted for a Blazor WebAssembly application.
    /// </summary>
    WebAssembly
}
