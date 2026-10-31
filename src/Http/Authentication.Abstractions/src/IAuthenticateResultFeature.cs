// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Used to capture the <see cref="AuthenticateResult"/> from the authorization middleware.
/// </summary>
public interface IAuthenticateResultFeature
{
    /// <summary>
    /// The <see cref="AuthenticateResult"/> from the authorization middleware.
    /// Set to null if the <see cref="IHttpAuthenticationFeature.User"/> property is set after the authorization middleware.
    /// </summary>
    AuthenticateResult? AuthenticateResult { get; set; }
}
