// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the response returned by an authenticator during the assertion phase of a WebAuthn login
/// process.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#authenticatorassertionresponse"/>.
/// </remarks>
internal sealed class AuthenticatorAssertionResponse(BufferSource authenticatorData, BufferSource signature, BufferSource? userHandle, BufferSource clientDataJSON) : AuthenticatorResponse(clientDataJSON)
{
    /// <summary>
    /// Gets or sets the authenticator data.
    /// </summary>
    public BufferSource AuthenticatorData { get; } = authenticatorData;

    /// <summary>
    /// Gets or sets the assertion signature.
    /// </summary>
    public BufferSource Signature { get; } = signature;

    /// <summary>
    /// Gets or sets the opaque user identifier.
    /// </summary>
    public BufferSource? UserHandle { get; } = userHandle;
}
