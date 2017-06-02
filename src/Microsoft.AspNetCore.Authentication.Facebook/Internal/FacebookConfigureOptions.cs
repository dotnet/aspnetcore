// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.AspNetCore.Authentication.Facebook.Internal
{
    public class FacebookConfigureOptions : ConfigureDefaultOptions<FacebookOptions>
    {
        public FacebookConfigureOptions(IConfiguration config) :
            base(FacebookDefaults.AuthenticationScheme,
                options => config.GetSection("Microsoft:AspNetCore:Authentication:Schemes:"+FacebookDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
