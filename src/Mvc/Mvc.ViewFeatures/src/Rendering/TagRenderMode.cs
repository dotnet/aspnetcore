// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Specifies constants for tag rendering modes.
/// </summary>
public enum TagRenderMode
{
    /// <summary>
    /// Normal mode.
    /// </summary>
    Normal,

    /// <summary>
    /// Start tag mode.
    /// </summary>
    StartTag,

    /// <summary>
    /// End tag mode.
    /// </summary>
    EndTag,

    /// <summary>
    /// Self closing mode.
    /// </summary>
    SelfClosing
}
