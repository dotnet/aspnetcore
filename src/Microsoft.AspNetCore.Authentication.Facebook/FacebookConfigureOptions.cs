// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Facebook
{
    internal class FacebookConfigureOptions : ConfigureNamedOptions<FacebookOptions>
    {
        public FacebookConfigureOptions(IConfiguration config) :
            base(FacebookDefaults.AuthenticationScheme,
                options => config.GetSection(FacebookDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
