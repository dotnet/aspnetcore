// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Validates the credential origin for passkey operations.
/// </summary>
public interface IPasskeyOriginValidator
{
    /// <summary>
    /// Determines whether the specified origin is valid for passkey operations.
    /// </summary>
    /// <param name="originInfo">Information about the passkey's origin.</param>
    /// <returns><c>true</c> if the origin is valid; otherwise, <c>false</c>.</returns>
    bool IsValidOrigin(PasskeyOriginInfo originInfo);
}
