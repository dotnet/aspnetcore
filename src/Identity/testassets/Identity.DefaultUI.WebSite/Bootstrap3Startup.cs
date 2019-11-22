// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.DefaultUI.WebSite
{
    public class Bootstrap3Startup : ApplicationUserStartup
    {
        public Bootstrap3Startup(IConfiguration configuration) : base(configuration)
        {
        }

        public override UIFramework Framework => UIFramework.Bootstrap3;
    }
}
