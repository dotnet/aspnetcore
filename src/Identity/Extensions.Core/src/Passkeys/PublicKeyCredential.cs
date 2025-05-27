// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents information about a public key/private key pair.
/// </summary>
/// <remarks>
/// See <see href="https://developer.mozilla.org/docs/Web/API/PublicKeyCredential" />
/// </remarks>
internal sealed class PublicKeyCredential<TResponse>(BufferSource id, string type, JsonElement clientExtensionResults, TResponse response)
    where TResponse : AuthenticatorResponse
{
    /// <summary>
    /// Gets or sets the credential ID.
    /// </summary>
    public BufferSource Id { get; } = id;

    /// <summary>
    /// Gets the type of the public key credential.
    /// </summary>
    /// <remarks>
    /// This is always expected to have the value <c>"public-key"</c>.
    /// </remarks>
    public string Type { get; } = type;

    /// <summary>
    /// Gets the client extensions map.
    /// </summary>
    public JsonElement ClientExtensionResults { get; } = clientExtensionResults;

    /// <summary>
    /// Gets or sets the authenticator response.
    /// </summary>
    public TResponse Response { get; } = response;

    /// <summary>
    /// Gets or sets a string indicating the mechanism by which the WebAuthn implementation
    /// is attached to the authenticator.
    /// </summary>
    public string? AuthenticatorAttachment { get; set; }
}
