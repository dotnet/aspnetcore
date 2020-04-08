// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ApiAuthorizationMessageHandlerConfiguration : IPostConfigureOptions<RemoteAuthenticationMessageHandlerOptions>
    {
        private readonly NavigationManager _navigationManager;

        public ApiAuthorizationMessageHandlerConfiguration(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public void PostConfigure(string name, RemoteAuthenticationMessageHandlerOptions options)
        {
            if (name == Options.Options.DefaultName && options.AllowedOrigins.Count == 0)
            {
                options.AllowedOrigins.Add(new Uri(new Uri(_navigationManager.Uri).GetLeftPart(UriPartial.Authority)));
            }
        }
    }
}
