// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a handler for passkey assertion and attestation.
/// </summary>
public interface IPasskeyHandler<TUser>
    where TUser : class
{
    /// <summary>
    /// Performs passkey attestation using the provided credential JSON and original options JSON.
    /// </summary>
    /// <param name="context">The context containing necessary information for passkey attestation.</param>
    /// <returns>A task object representing the asynchronous operation containing the <see cref="PasskeyAttestationResult"/>.</returns>
    Task<PasskeyAttestationResult> PerformAttestationAsync(PasskeyAttestationContext<TUser> context);

    /// <summary>
    /// Performs passkey assertion using the provided credential JSON, original options JSON, and optional user.
    /// </summary>
    /// <param name="context">The context containing necessary information for passkey assertion.</param>
    /// <returns>A task object representing the asynchronous operation containing the <see cref="PasskeyAssertionResult{TUser}"/>.</returns>
    Task<PasskeyAssertionResult<TUser>> PerformAssertionAsync(PasskeyAssertionContext<TUser> context);
}
