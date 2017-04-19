// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount
{
    internal class MicrosoftAccountConfigureOptions : ConfigureNamedOptions<MicrosoftAccountOptions>
    {
        // Bind to "Microsoft" section by default
        public MicrosoftAccountConfigureOptions(IConfiguration config) :
            base(MicrosoftAccountDefaults.AuthenticationScheme,
                options => config.GetSection(MicrosoftAccountDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
