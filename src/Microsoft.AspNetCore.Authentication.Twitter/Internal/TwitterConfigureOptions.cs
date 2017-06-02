// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.AspNetCore.Authentication.Twitter.Internal
{
    public class TwitterConfigureOptions : ConfigureDefaultOptions<TwitterOptions>
    {
        // Bind to "Twitter" section by default
        public TwitterConfigureOptions(IConfiguration config) :
            base(TwitterDefaults.AuthenticationScheme,
                options => config.GetSection("Microsoft:AspNetCore:Authentication:Schemes:"+TwitterDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
