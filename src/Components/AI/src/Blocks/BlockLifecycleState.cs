// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Describes where a <see cref="ContentBlock"/> is in its streaming lifecycle.
/// </summary>
public enum BlockLifecycleState
{
    /// <summary>
    /// The block has been created but has not been emitted to the conversation yet.
    /// </summary>
    Pending,

    /// <summary>
    /// The block is part of the conversation and can still receive updates from the model stream.
    /// </summary>
    Active,

    /// <summary>
    /// The block is complete and will not receive further updates.
    /// </summary>
    Inactive,
}
