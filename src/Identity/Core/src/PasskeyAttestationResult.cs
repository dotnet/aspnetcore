// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of a passkey attestation operation.
/// </summary>
public sealed class PasskeyAttestationResult
{
    /// <summary>
    /// Gets whether the attestation was successful.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Passkey))]
    [MemberNotNullWhen(false, nameof(Failure))]
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the passkey information collected during attestation when successful.
    /// </summary>
    public UserPasskeyInfo? Passkey { get; }

    /// <summary>
    /// Gets the error that occurred during attestation.
    /// </summary>
    public PasskeyException? Failure { get; }

    private PasskeyAttestationResult(UserPasskeyInfo passkey)
    {
        Succeeded = true;
        Passkey = passkey;
    }

    private PasskeyAttestationResult(PasskeyException failure)
    {
        Succeeded = false;
        Failure = failure;
    }

    /// <summary>
    /// Creates a successful result for a passkey attestation operation.
    /// </summary>
    /// <param name="passkey">The passkey information associated with the attestation.</param>
    /// <returns>A <see cref="PasskeyAttestationResult"/> instance representing a successful attestation.</returns>
    public static PasskeyAttestationResult Success(UserPasskeyInfo passkey)
    {
        ArgumentNullException.ThrowIfNull(passkey);
        return new PasskeyAttestationResult(passkey);
    }

    /// <summary>
    /// Creates a failed result for a passkey attestation operation.
    /// </summary>
    /// <param name="failure">The exception that describes the reason for the failure.</param>
    /// <returns>A <see cref="PasskeyAttestationResult"/> instance representing the failure.</returns>
    public static PasskeyAttestationResult Fail(PasskeyException failure)
    {
        return new PasskeyAttestationResult(failure);
    }
}
