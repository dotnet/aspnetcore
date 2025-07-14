// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents information about the user associated with a passkey.
/// </summary>
/// <param name="id">The user ID.</param>
/// <param name="name">The name of the user.</param>
/// <param name="displayName">The display name of the user. When omitted, defaults to the user's name.</param>
public sealed class PasskeyUserEntity(string id, string name, string? displayName)
{
    /// <summary>
    /// Gets the user ID associated with a passkey.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// Gets the name of the user associated with a passkey.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the display name of the user associated with a passkey.
    /// </summary>
    public string DisplayName { get; } = displayName ?? name;
}
