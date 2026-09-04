// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Caching.SqlServer;

public class SqlServerCacheServicesExtensionsTest
{
    [Fact]
    public void AddDistributedSqlServerCache_AddsAsSingleRegistrationService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        SqlServerCachingServicesExtensions.AddSqlServerCacheServices(services);

        // Assert
        var serviceDescriptor = Assert.Single(services);
        Assert.Equal(typeof(IDistributedCache), serviceDescriptor.ServiceType);
        Assert.Equal(typeof(SqlServerCache), serviceDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddDistributedSqlServerCache_ReplacesPreviouslyUserRegisteredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped(typeof(IDistributedCache), sp => Mock.Of<IDistributedCache>());

        // Act
        services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = "Fake";
            options.SchemaName = "Fake";
            options.TableName = "Fake";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var distributedCache = services.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));

        Assert.NotNull(distributedCache);
        Assert.Equal(ServiceLifetime.Scoped, distributedCache.Lifetime);
        Assert.IsType<SqlServerCache>(serviceProvider.GetRequiredService<IDistributedCache>());
    }

    [Fact]
    public void AddDistributedSqlServerCache_allows_chaining()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddDistributedSqlServerCache(_ => { }));
    }

    [Fact]
    public void AddDistributedSqlServerCache_ConnectionFactory_UsedInsteadOfConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        var factoryInvoked = false;

        // Act
        // ConnectionFactory can be set directly in the options action.
        // The factory closes over whatever it needs at registration time —
        // no IServiceProvider injection required on the cache itself.
        services.AddDistributedSqlServerCache(options =>
        {
            options.SchemaName = "dbo";
            options.TableName = "Cache";
            options.ConnectionFactory = () =>
            {
                factoryInvoked = true;
                return new SqlConnection("Server=fake");
            };
        });

        // Assert: service resolves (factory is not invoked until a cache operation is performed)
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();
        Assert.IsType<SqlServerCache>(cache);
        Assert.False(factoryInvoked);
    }

    [Fact]
    public void AddDistributedSqlServerCache_ConnectionFactory_CanCaptureServicesViaIConfigureOptions()
    {
        // Arrange
        // IConfigureOptions<T> is the idiomatic way to inject other services into options
        // when the factory itself depends on a registered service (e.g. a token provider).
        var services = new ServiceCollection();
        services.AddSingleton<IFakeTokenService, FakeTokenService>();
        services.AddDistributedSqlServerCache(_ => { });

        services.AddOptions<SqlServerCacheOptions>()
            .Configure<IFakeTokenService>((options, tokenService) =>
            {
                options.SchemaName = "dbo";
                options.TableName = "Cache";
                options.ConnectionFactory = () =>
                {
                    var conn = new SqlConnection("Server=fake");
                    conn.AccessToken = tokenService.GetToken();
                    return conn;
                };
            });

        // Assert: service resolves and the token service was captured in the factory closure
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();
        Assert.IsType<SqlServerCache>(cache);
    }

    private interface IFakeTokenService
    {
        string GetToken();
    }

    private sealed class FakeTokenService : IFakeTokenService
    {
        public string GetToken() => "fake-token";
    }
}
