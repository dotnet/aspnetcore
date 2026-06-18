// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Internal marker interface used by the renderer to indicate
/// whether a component participates in server prerendering.
///
/// NOTE:
/// This interface is INTERNAL and must never be exposed as part of
/// the public API surface. Consumers must not rely on its members
/// directly.
/// </summary>
internal interface IPrerenderMode
{
    /// <summary>
    /// Indicates whether server prerendering is enabled.
    /// This value must only be interpreted by the renderer.
    /// </summary>
    bool Prerender { get; }
}

