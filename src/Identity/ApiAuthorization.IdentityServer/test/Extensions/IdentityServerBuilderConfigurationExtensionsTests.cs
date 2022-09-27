// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Extensions;

public static class IdentityServerBuilderConfigurationExtensionsTests
{
    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/6720", Queues = "All.OSX")]
    public static void IValidationKeysStore_Service_Resolution_Succeeds_If_Key_Found()
    {
        // Arrange
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["IdentityServer:Key:Type"] = "File",
                ["IdentityServer:Key:FilePath"] = "test.pfx",
                ["IdentityServer:Key:Password"] = "aspnetcore"
            }).Build();

        IWebHostEnvironment environment = new MyWebHostEnvironment();

        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddSingleton(environment)
            .AddOptions();

        services.AddDefaultIdentity<MyUser>();

        services.AddIdentityServer()
                .AddApiAuthorization<MyUser, MyUserContext>();

        services.AddAuthentication();

        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var store = serviceProvider.GetRequiredService<IValidationKeysStore>();

        // Assert
        Assert.NotNull(store);
    }

    [Fact]
    public static void IValidationKeysStore_Service_Resolution_Fails_If_No_Signing_Credential_Configured()
    {
        // Arrange
        IConfiguration configuration = new ConfigurationBuilder()
            .Build();

        IWebHostEnvironment environment = new MyWebHostEnvironment();

        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddSingleton(environment)
            .AddOptions();

        services.AddDefaultIdentity<MyUser>();

        services.AddIdentityServer()
                .AddApiAuthorization<MyUser, MyUserContext>();

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

        public DbSet<Key> Keys { get; set; }

        public Task<int> SaveChangesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
