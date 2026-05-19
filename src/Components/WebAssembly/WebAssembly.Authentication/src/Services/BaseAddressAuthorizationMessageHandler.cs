// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches access tokens to outgoing <see cref="HttpResponseMessage"/> instances.
/// Access tokens will only be added when the request URI is within the application's base URI.
/// </summary>
public class BaseAddressAuthorizationMessageHandler : AuthorizationMessageHandler
{
    /// <summary>
    /// Initializes a new instance of <see cref="BaseAddressAuthorizationMessageHandler"/>.
    /// </summary>
    /// <param name="provider">The <see cref="IAccessTokenProvider"/> to use for requesting tokens.</param>
    /// <param name="navigationManager">The <see cref="NavigationManager"/> used to compute the base address.</param>
    public BaseAddressAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigationManager)
        : base(provider, navigationManager)
    {
        ConfigureHandler(new[] { navigationManager.BaseUri });
    }
}
