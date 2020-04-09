// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class BaseAddressAuthenticationMessageHandler : RemoteAuthenticationMessageHandler
    {
        public BaseAddressAuthenticationMessageHandler(IAccessTokenProvider provider, NavigationManager navigationManager) : base(provider)
            => UseAllowedUrls(navigationManager.BaseUri);
    }
}
