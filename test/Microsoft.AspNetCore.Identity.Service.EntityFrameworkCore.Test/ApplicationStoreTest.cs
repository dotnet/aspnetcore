// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;
using Microsoft.AspNetCore.Identity.Service.Specification.Tests;
using Microsoft.AspNetCore.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore.Test
{
    public class ApplicationStoreTest : IdentityServiceSpecificationTestBase<IdentityUser, IdentityServiceApplication>, IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ScratchDatabaseFixture _fixture;

        public ApplicationStoreTest(ScratchDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        protected override void AddApplicationStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IApplicationStore<IdentityServiceApplication>>(
                new ApplicationStore<IdentityServiceApplication, IdentityServiceScope<string>, IdentityServiceApplicationClaim<string>, IdentityServiceRedirectUri<string>, IdentityServiceDbContext<IdentityUser, IdentityServiceApplication>, string, string>((IdentityServiceDbContext<IdentityUser, IdentityServiceApplication>)context));
        }

        public IdentityServiceDbContext<IdentityUser, IdentityServiceApplication> CreateContext(bool delete = false)
        {
            var db = DbUtil.Create<TestContext>(_fixture.ConnectionString);
            if (delete)
            {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }

        protected override IdentityServiceApplication CreateTestApplication()
        {
            return new IdentityServiceApplication
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString()
            };
        }

        protected override object CreateTestContext()
        {
            return CreateContext();
        }

        private class TestContext : IdentityServiceDbContext<IdentityUser, IdentityServiceApplication>
        {
            public TestContext(DbContextOptions options) : base(options) { }
        }

        protected override bool ShouldSkipDbTests() => TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
    }
}
