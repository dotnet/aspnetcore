// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test;

public static class TestIdentityFactory
{
    private static InMemoryContext CreateContext(SqliteConnection connection)
        => InMemoryContext.Create(connection);

    public static IServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddLogging();
        services.AddIdentity<IdentityUser, IdentityRole>();
        return services;
    }

    public static RoleManager<IdentityRole> CreateRoleManager(InMemoryContext context)
    {
        var services = CreateTestServices();
        services.AddSingleton<IRoleStore<IdentityRole>>(new RoleStore<IdentityRole>(context));
        return services.BuildServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
    }

    public static RoleManager<IdentityRole> CreateRoleManager(SqliteConnection connection)
        => CreateRoleManager(CreateContext(connection));
}
