// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents arguments for generating <see cref="PasskeyRequestOptions"/>.
/// </summary>
public sealed class PasskeyRequestArgs<TUser>
    where TUser : class
{
    /// <summary>
    /// Gets or sets the user verification requirement.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialrequestoptions-userverification"/>.
    /// Possible values are "required", "preferred", and "discouraged".
    /// The default value is "preferred".
    /// </remarks>
    public string UserVerification { get; set; } = "preferred";

    /// <summary>
    /// Gets or sets the user to be authenticated.
    /// </summary>
    /// <remarks>
    /// While this value is optional, it should be specified if the authenticating
    /// user can be identified. This can happen if, for example, the user provides
    /// a username before signing in with a passkey.
    /// </remarks>
    public TUser? User { get; set; }

    /// <summary>
    /// Gets or sets the client extension inputs.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialrequestoptions-extensions"/>.
    /// </remarks>
    public JsonElement? Extensions { get; set; }
}
