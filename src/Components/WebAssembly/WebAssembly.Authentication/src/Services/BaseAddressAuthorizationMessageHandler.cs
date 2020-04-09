// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class BaseAddressAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        public BaseAddressAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigationManager) : base(provider)
        {
            ConfigureHandler(new[] { navigationManager.BaseUri });
            InnerHandler = new HttpClientHandler();
        }
    }
}
