// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Identity.OpenIdConnect.WebSite.Identity.Data;
using Identity.OpenIdConnect.WebSite.Identity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.AspnetCore.Identity.Service.FunctionalTests
{
    public class EntityFrameworkSeedReferenceData : IStartupFilter
    {
        public EntityFrameworkSeedReferenceData(
            IdentityServiceDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ReferenceData seedData)
        {
            SeedContext(dbContext, userManager, seedData);
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return next;
        }

        private void SeedContext(IdentityServiceDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ReferenceData seedData)
        {
            foreach (var userAndPassword in seedData.UsersAndPasswords)
            {
                userManager
                    .CreateAsync(userAndPassword.user, userAndPassword.password)
                    .GetAwaiter()
                    .GetResult();
            }

            dbContext.Applications.AddRange(seedData.ClientApplications);
            dbContext.SaveChanges();
        }
    }
}
