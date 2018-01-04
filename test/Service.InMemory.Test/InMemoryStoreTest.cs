// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity.Service.Specification.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.Service.InMemory.Test
{
    public class InMemoryStoreTest : IdentityServiceSpecificationTestBase<TestUser, TestApplication>
    {
        protected override void AddApplicationStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IApplicationStore<TestApplication>>((InMemoryStore<TestApplication>)context);
        }

        protected override TestApplication CreateTestApplication()
        {
            return new TestApplication
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString()
            };
        }

        protected override object CreateTestContext()
        {
            return new InMemoryStore<TestApplication>();
        }
    }
}
