// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.DefaultUI.WebSite
{
    public class Startup : StartupBase<IdentityUser, IdentityDbContext>
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
