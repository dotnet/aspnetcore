// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Extensions
{
    public static class IdentityServerBuilderConfigurationExtensionsTests
    {
        [Fact]
        public static void IValidationKeysStore_Service_Resolution_Fails_If_No_Signing_Credential_Configured()
        {
            // Arrange
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["MyAPI:Profile"] = "API" })
                .Build();

            IWebHostEnvironment environment = new MyWebHostEnvironment();

            var services = new ServiceCollection()
                .AddSingleton(configuration)
                .AddSingleton(environment)
                .AddOptions();

            services.AddDefaultIdentity<MyUser>();

            services.AddIdentityServer()
                    .AddApiAuthorization<MyUser, MyUserContext>();

            services.AddAuthentication()
                    .AddIdentityServerJwt();

            using var serviceProvider = services.BuildServiceProvider();

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => serviceProvider.GetRequiredService<IValidationKeysStore>());

            Assert.Equal("No signing credential is configured by the 'IdentityServer:Key' configuration section.", exception.Message);
        }

        private class MyWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
        }

        private class MyUser
        {
            public string Id { get; set; }
        }

        private class MyUserContext : DbContext, IPersistedGrantDbContext
        {
            public MyUserContext(DbContextOptions options)
                : base(options)
            {
            }

            public DbSet<PersistedGrant> PersistedGrants { get; set; }

            public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

            public Task<int> SaveChangesAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
