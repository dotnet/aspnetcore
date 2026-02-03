// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a handler for generating passkey creation and request options and performing
/// passkey assertion and attestation.
/// </summary>
public interface IPasskeyHandler<TUser>
    where TUser : class
{
    /// <summary>
    /// Generates passkey creation options for the specified user entity and HTTP context.
    /// </summary>
    /// <param name="userEntity">The passkey user entity for which to generate creation options.</param>
    /// <param name="httpContext">The HTTP context associated with the request.</param>
    /// <returns>A <see cref="PasskeyCreationOptionsResult"/> representing the result.</returns>
    Task<PasskeyCreationOptionsResult> MakeCreationOptionsAsync(PasskeyUserEntity userEntity, HttpContext httpContext);

    /// <summary>
    /// Generates passkey request options for the specified user and HTTP context.
    /// </summary>
    /// <param name="user">The user for whom to generate request options.</param>
    /// <param name="httpContext">The HTTP context associated with the request.</param>
    /// <returns>A <see cref="PasskeyRequestOptionsResult"/> representing the result.</returns>
    Task<PasskeyRequestOptionsResult> MakeRequestOptionsAsync(TUser? user, HttpContext httpContext);

    /// <summary>
    /// Performs passkey attestation using the provided <see cref="PasskeyAttestationContext"/>.
    /// </summary>
    /// <param name="context">The context containing necessary information for passkey attestation.</param>
    /// <returns>A <see cref="PasskeyAttestationResult"/> representing the result.</returns>
    Task<PasskeyAttestationResult> PerformAttestationAsync(PasskeyAttestationContext context);

    /// <summary>
    /// Performs passkey assertion using the provided <see cref="PasskeyAssertionContext"/>.
    /// </summary>
    /// <param name="context">The context containing necessary information for passkey assertion.</param>
    /// <returns>A <see cref="PasskeyAssertionResult{TUser}"/> representing the result.</returns>
    Task<PasskeyAssertionResult<TUser>> PerformAssertionAsync(PasskeyAssertionContext context);
}
