// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public static class FunctionalTestsServiceCollectionExtensions
    {
        public static IServiceCollection SetupTestDatabase(this IServiceCollection services, string databaseName)
        {
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase(databaseName, memoryOptions => { }));

            return services;
        }
    }
}
