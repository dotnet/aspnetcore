// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Specifies the reason for persistent component state restoration in web scenarios.
/// </summary>
[Flags]
public enum WebPersistenceReason
{
    /// <summary>
    /// State restoration during prerendering.
    /// </summary>
    Prerendering = 1,

    /// <summary>
    /// State restoration during enhanced navigation.
    /// </summary>
    EnhancedNavigation = 2,

    /// <summary>
    /// State restoration after server reconnection.
    /// </summary>
    Reconnection = 4
}