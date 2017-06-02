// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount.Internal
{
    public class MicrosoftAccountConfigureOptions : ConfigureDefaultOptions<MicrosoftAccountOptions>
    {
        // Bind to "Microsoft" section by default
        public MicrosoftAccountConfigureOptions(IConfiguration config) :
            base(MicrosoftAccountDefaults.AuthenticationScheme,
                options => config.GetSection("Microsoft:AspNetCore:Authentication:Schemes:"+MicrosoftAccountDefaults.AuthenticationScheme).Bind(options))
        { }
    }
}
