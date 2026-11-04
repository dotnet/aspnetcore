// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents the options available for a restore operation.
/// </summary>
public readonly struct RestoreOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreOptions"/> struct with default values.
    /// </summary>
    public RestoreOptions()
    {
    }

    /// <summary>
    /// Gets the behavior to use when restoring data.
    /// </summary>
    public RestoreBehavior RestoreBehavior { get; init; } = RestoreBehavior.Default;

    /// <summary>
    /// Gets a value indicating whether the registration wants to receive updates beyond
    /// the initially provided value.
    /// </summary>
    public bool AllowUpdates { get; init; } = false;
}
