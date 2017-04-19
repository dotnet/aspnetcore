// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Google
{
    internal class GoogleConfigureOptions : ConfigureNamedOptions<GoogleOptions>
    {
        public GoogleConfigureOptions(IConfiguration config) :
            base(GoogleDefaults.AuthenticationScheme,
                options => config.GetSection(GoogleDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
