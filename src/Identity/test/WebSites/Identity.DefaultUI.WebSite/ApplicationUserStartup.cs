// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Identity.DefaultUI.WebSite.Data;
using Microsoft.Extensions.Configuration;

namespace Identity.DefaultUI.WebSite
{
    public class ApplicationUserStartup : StartupBase<ApplicationUser, ApplicationDbContext>
    {
        public ApplicationUserStartup(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
