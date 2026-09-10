// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents information about the user associated with a passkey.
/// </summary>
public sealed class PasskeyUserEntity
{
    /// <summary>
    /// Gets the user ID associated with a passkey.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the name of the user associated with a passkey.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the display name of the user associated with a passkey.
    /// </summary>
    public required string DisplayName { get; init; }
}
