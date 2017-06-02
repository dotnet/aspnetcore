// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Internal
{
    public class JwtBearerConfigureOptions : ConfigureDefaultOptions<JwtBearerOptions>
    {
        // Bind to "Bearer" section by default
        public JwtBearerConfigureOptions(IConfiguration config) :
            base(JwtBearerDefaults.AuthenticationScheme,
                options => config.GetSection("Microsoft:AspNetCore:Authentication:Schemes:"+JwtBearerDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
