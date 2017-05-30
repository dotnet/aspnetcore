// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.Service.Specification.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore.InMemory.Test
{
    public class InMemoryEFApplicationStoreTest : IdentityServiceSpecificationTestBase<IdentityUser, IdentityServiceApplication, string, string>
    {
        protected override void AddApplicationStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IApplicationStore<IdentityServiceApplication>>(
                new ApplicationStore<IdentityServiceApplication, IdentityServiceScope<string>, IdentityServiceApplicationClaim<string>, IdentityServiceRedirectUri<string>, InMemoryContext, string, string>((InMemoryContext)context, new ApplicationErrorDescriber()));
        }

        protected override IdentityServiceApplication CreateTestApplication()
        {
            return new IdentityServiceApplication
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
            };
        }

        protected override object CreateTestContext()
        {
            return new InMemoryContext(new DbContextOptionsBuilder().Options);
        }
    }
}
