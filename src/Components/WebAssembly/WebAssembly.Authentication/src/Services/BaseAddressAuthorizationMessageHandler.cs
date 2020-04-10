// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that will attach authorization tokens using the default scopes to
    /// outgoing requests where the application base address is a base of the target request url.
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
}
