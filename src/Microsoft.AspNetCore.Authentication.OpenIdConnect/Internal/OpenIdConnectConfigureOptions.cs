// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect.Internal
{
    internal class OpenIdConnectConfigureOptions : ConfigureDefaultOptions<OpenIdConnectOptions>
    {
        // Bind to "OpenIdConnect" section by default
        public OpenIdConnectConfigureOptions(IConfiguration config) :
            base(OpenIdConnectDefaults.AuthenticationScheme,
                options => config.GetSection("Microsoft:AspNetCore:Authentication:Schemes:"+OpenIdConnectDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
