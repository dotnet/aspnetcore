
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Specifies where remote authentication tokens and other related state are stored in the browser.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RemoteAuthenticationTokenStorage>))]
public enum RemoteAuthenticationTokenStorage
{
    /// <summary>
    /// Stores tokens in browser session storage. Cleared when the tab or window is closed and not shared across tabs.
    /// </summary>
    SessionStorage = 0,

    /// <summary>
    /// Stores tokens in browser local storage. Persists across browser sessions, tabs, and windows for the same origin.
    /// </summary>
    LocalStorage = 1
}
