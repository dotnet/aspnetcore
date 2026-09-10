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
    /// Gets a value indicating whether this handler supports generating passkey signal options.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="false"/> unless the handler implements
    /// <see cref="MakeAllAcceptedCredentialsSignalOptionsAsync(TUser, HttpContext)"/> and
    /// <see cref="MakeCurrentUserDetailsSignalOptionsAsync(TUser, PasskeyUserEntity, HttpContext)"/>
    /// and can retrieve the user's passkeys.
    /// </remarks>
    bool SupportsPasskeySignalOptions => false;

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
    /// Generates the options used to signal the credentials that are currently registered for a user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Handlers that implement this method should also return <see langword="true"/> from
    /// <see cref="SupportsPasskeySignalOptions"/>. See <see href="https://www.w3.org/TR/webauthn-3/#sctn-signal-methods"/>.
    /// </para>
    /// <para>
    /// The generated options reveal the user's credential IDs, so only generate them for the
    /// currently authenticated user.
    /// </para>
    /// </remarks>
    /// <param name="user">The user whose passkeys should be signaled.</param>
    /// <param name="httpContext">The HTTP context associated with the request.</param>
    /// <returns>An <see cref="AllAcceptedCredentialsSignalOptionsResult"/> representing the result.</returns>
    /// <exception cref="NotSupportedException">Thrown when the handler does not support generating passkey signal options.</exception>
    Task<AllAcceptedCredentialsSignalOptionsResult> MakeAllAcceptedCredentialsSignalOptionsAsync(TUser user, HttpContext httpContext)
        => throw new NotSupportedException($"'{GetType()}' does not support generating passkey signal options.");

    /// <summary>
    /// Generates the options used to signal the current details of a user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Handlers that implement this method should also return <see langword="true"/> from
    /// <see cref="SupportsPasskeySignalOptions"/>. See <see href="https://www.w3.org/TR/webauthn-3/#sctn-signal-methods"/>.
    /// </para>
    /// <para>
    /// The generated options reveal the user's details, so only generate them for the
    /// currently authenticated user.
    /// </para>
    /// </remarks>
    /// <param name="user">The user whose details should be signaled.</param>
    /// <param name="userEntity">
    /// The passkey user entity associated with the user's passkeys. Its <see cref="PasskeyUserEntity.Id"/>
    /// must match the ID of <paramref name="user"/>. The <see cref="PasskeyUserEntity.Name"/> and
    /// <see cref="PasskeyUserEntity.DisplayName"/> are the values being signaled.
    /// </param>
    /// <param name="httpContext">The HTTP context associated with the request.</param>
    /// <returns>A <see cref="CurrentUserDetailsSignalOptionsResult"/> representing the result.</returns>
    /// <exception cref="NotSupportedException">Thrown when the handler does not support generating passkey signal options.</exception>
    Task<CurrentUserDetailsSignalOptionsResult> MakeCurrentUserDetailsSignalOptionsAsync(TUser user, PasskeyUserEntity userEntity, HttpContext httpContext)
        => throw new NotSupportedException($"'{GetType()}' does not support generating passkey signal options.");

    /// <summary>
    /// Generates options used to signal that a passkey credential is unknown to the server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The signal permanently deletes the passkey from the browser's passkey provider. A handler must only return
    /// options when the credential is not registered to any user on the server.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#sctn-signal-methods"/>.
    /// </para>
    /// <para>
    /// Unlike <see cref="MakeAllAcceptedCredentialsSignalOptionsAsync(TUser, HttpContext)"/>, an incorrect signal
    /// may permanently delete a working passkey. A handler must only return options after conclusively determining
    /// that the credential is not registered to any user. An inconclusive lookup must not produce signal options.
    /// </para>
    /// </remarks>
    /// <param name="credentialJson">The JSON representation of the passkey credential.</param>
    /// <param name="httpContext">The HTTP context associated with the request.</param>
    /// <returns>
    /// An <see cref="UnknownCredentialSignalOptionsResult"/> when the credential is unknown to the server,
    /// otherwise <see langword="null"/>.
    /// </returns>
    Task<UnknownCredentialSignalOptionsResult?> MakeUnknownCredentialSignalOptionsAsync(string credentialJson, HttpContext httpContext)
        => Task.FromResult<UnknownCredentialSignalOptionsResult?>(null);

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
