// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// Lists the different authentication methods used to
/// redirect the user agent to the identity provider.
/// </summary>
public enum OpenIdConnectRedirectBehavior
{
    /// <summary>
    /// Emits a 302 response to redirect the user agent to
    /// the OpenID Connect provider using a GET request.
    /// </summary>
    RedirectGet = 0,

    /// <summary>
    /// Emits an HTML form to redirect the user agent to
    /// the OpenID Connect provider using a POST request.
    /// </summary>
    FormPost = 1
}
