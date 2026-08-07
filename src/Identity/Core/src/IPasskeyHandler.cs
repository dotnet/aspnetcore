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
    /// Gets a value indicating whether this handler supports conditionally mediated passkey creation.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="false"/> unless the handler implements
    /// <see cref="MakeCreationOptionsAsync(PasskeyUserEntity, bool, HttpContext)"/>.
    /// </remarks>
    bool SupportsConditionalCreation => false;

    /// <summary>
    /// Generates passkey creation options for the specified user entity and HTTP context.
    /// </summary>
    /// <param name="userEntity">The passkey user entity for which to generate creation options.</param>
    /// <param name="httpContext">The HTTP context associated with the request.</param>
    /// <returns>A <see cref="PasskeyCreationOptionsResult"/> representing the result.</returns>
    Task<PasskeyCreationOptionsResult> MakeCreationOptionsAsync(PasskeyUserEntity userEntity, HttpContext httpContext);

    /// <summary>
    /// Generates passkey creation options for the specified user entity and HTTP context.
    /// </summary>
    /// <param name="userEntity">The passkey user entity for which to generate creation options.</param>
    /// <param name="isConditionallyMediated">
    /// <see langword="true"/> if the passkey will be created with conditional mediation; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="httpContext">The HTTP context associated with the request.</param>
    /// <returns>A <see cref="PasskeyCreationOptionsResult"/> representing the result.</returns>
    /// <remarks>
    /// Conditional mediation lets a passkey be created without a user gesture, typically immediately
    /// after the user signs in with a password. The corresponding <c>navigator.credentials.create()</c>
    /// call must specify <c>mediation: "conditional"</c>.
    /// The caller must only request conditional mediation after a recent successful password authentication.
    /// An existing authenticated session by itself is not sufficient authorization to add a new passkey.
    /// The protected attestation state prevents the client from changing the mediation mode after options
    /// are issued, but it does not authorize issuing conditional options.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="isConditionallyMediated"/> is <see langword="true"/> and the handler
    /// does not support conditionally mediated passkey creation.
    /// </exception>
    Task<PasskeyCreationOptionsResult> MakeCreationOptionsAsync(PasskeyUserEntity userEntity, bool isConditionallyMediated, HttpContext httpContext)
    {
        if (isConditionallyMediated)
        {
            throw new NotSupportedException(
                $"The passkey handler '{GetType()}' does not support conditionally mediated passkey creation.");
        }

        return MakeCreationOptionsAsync(userEntity, httpContext);
    }

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
