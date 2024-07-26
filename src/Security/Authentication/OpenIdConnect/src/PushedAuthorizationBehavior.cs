// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// Enum containing the options for use of Pushed Authorization (PAR).
/// </summary>
public enum PushedAuthorizationBehavior
 {
    /// <summary>
    /// Use Pushed Authorization (PAR) if the PAR endpoint is available in the identity provider's discovery document or the explicit <see cref="OpenIdConnectConfiguration"/>. This is the default value.
    /// </summary>
    UseIfAvailable,
    /// <summary>
    /// Never use Pushed Authorization (PAR), even if the PAR endpoint is available in the identity provider's discovery document or the explicit <see cref="OpenIdConnectConfiguration"/>.
    /// If the identity provider's discovery document indicates that it requires Pushed Authorization (PAR), the handler will fail.
    /// </summary>
    Disable,
    /// <summary>
    /// Always use Pushed Authorization (PAR), and emit errors if the PAR endpoint is not available in the identity provider's discovery document or the explicit <see cref="OpenIdConnectConfiguration"/>.
    /// </summary>
    Require
 }
