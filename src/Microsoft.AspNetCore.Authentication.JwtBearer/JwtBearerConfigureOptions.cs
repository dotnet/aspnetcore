// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    internal class JwtBearerConfigureOptions : ConfigureNamedOptions<JwtBearerOptions>
    {
        // Bind to "Bearer" section by default
        public JwtBearerConfigureOptions(IConfiguration config) :
            base(JwtBearerDefaults.AuthenticationScheme,
                options => config.GetSection(JwtBearerDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
