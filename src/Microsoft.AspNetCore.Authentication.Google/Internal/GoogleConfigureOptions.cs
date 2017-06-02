// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.AspNetCore.Authentication.Google.Internal
{
    public class GoogleConfigureOptions : ConfigureDefaultOptions<GoogleOptions>
    {
        public GoogleConfigureOptions(IConfiguration config) :
            base(GoogleDefaults.AuthenticationScheme,
                options => config.GetSection("Microsoft:AspNetCore:Authentication:Schemes:"+GoogleDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
