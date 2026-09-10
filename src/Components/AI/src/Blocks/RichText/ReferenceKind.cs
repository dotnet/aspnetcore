// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Describes the syntax used by a reference.
/// </summary>
public enum ReferenceKind
{
    /// <summary>
    /// A shortcut reference.
    /// </summary>
    Shortcut,

    /// <summary>
    /// A collapsed reference.
    /// </summary>
    Collapsed,

    /// <summary>
    /// A full reference.
    /// </summary>
    Full,
}
