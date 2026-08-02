// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Types in the Microsoft.AspNetCore.Components.RenderTree namespace are not recommended for use outside
/// of the Blazor framework. These types will change in future release.
/// </summary>
[Flags]
public enum ComponentFrameFlags : byte
{
    /// <summary>
    /// Indicates that the caller has specified a render mode.
    /// </summary>
    HasCallerSpecifiedRenderMode = 1,
}
