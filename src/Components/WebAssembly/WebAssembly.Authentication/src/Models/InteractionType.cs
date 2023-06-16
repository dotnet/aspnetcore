// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// The type of authentication request.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<InteractionType>))]
public enum InteractionType
{
    /// <summary>
    /// Authenticating or reauthenticating the user and provisioning the default access token.
    /// </summary>
    SignIn,

    /// <summary>
    /// Provisioning a token interactively because silent provisioning failed, either because the end user
    /// has not consented or because the existing credentials have expired.
    /// </summary>
    GetToken,

    /// <summary>
    /// Logging the user out.
    /// </summary>
    SignOut,
}
