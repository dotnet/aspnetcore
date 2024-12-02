// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public class DefaultPocoTest : IClassFixture<ScratchDatabaseFixture>
{
    private readonly ApplicationBuilder _builder;

    public DefaultPocoTest(ScratchDatabaseFixture fixture)
    {
        var services = new ServiceCollection();

        services
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddDbContext<IdentityDbContext>(o =>
                o.UseSqlite(fixture.Connection)
                    .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning)))
            .AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<IdentityDbContext>();

        services.AddLogging();

        var provider = services.BuildServiceProvider();
        _builder = new ApplicationBuilder(provider);

        using (var scoped = provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            scoped.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.EnsureCreated();
        }
    }

    [ConditionalFact]
    public async Task EnsureStartupUsageWorks()
    {
        var userStore = _builder.ApplicationServices.GetRequiredService<IUserStore<IdentityUser>>();
        var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();

        Assert.NotNull(userStore);
        Assert.NotNull(userManager);

        const string userName = "admin";
        const string password = "[PLACEHOLDER]-1a";
        var user = new IdentityUser { UserName = userName };
        IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
        IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
    }
}
