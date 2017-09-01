// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Identity.OpenIdConnect.WebSite.Identity.Models;

namespace Identity.OpenIdConnect.WebSite.Identity.Data
{
    public class IdentityServiceDbContext : IdentityServiceDbContext<ApplicationUser, IdentityServiceApplication>
    {
        public IdentityServiceDbContext(DbContextOptions<IdentityServiceDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
