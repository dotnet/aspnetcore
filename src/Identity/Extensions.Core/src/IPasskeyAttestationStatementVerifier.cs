// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Interface for verifying passkey attestation statements.
/// </summary>
public interface IPasskeyAttestationStatementVerifier
{
    /// <summary>
    /// Verifies the attestation statement of a passkey.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#verification-procedure"/>.
    /// </remarks>
    /// <param name="attestationObject">The attestation object to verify. See <see href="https://www.w3.org/TR/webauthn-3/#attestation-object"/>.</param>
    /// <param name="clientDataHash">The hash of the client data used during registration.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the verification is successful; otherwise, false.</returns>
    Task<bool> VerifyAsync(ReadOnlyMemory<byte> attestationObject, ReadOnlyMemory<byte> clientDataHash);
}
