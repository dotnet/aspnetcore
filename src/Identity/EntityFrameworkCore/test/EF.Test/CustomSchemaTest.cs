// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public class CustomSchemaTest : IClassFixture<ScratchDatabaseFixture>
{
    private readonly ApplicationBuilder _builder;

    public CustomSchemaTest(ScratchDatabaseFixture fixture)
    {
        var services = new ServiceCollection();
        services
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddDbContext<CustomVersionDbContext>(o =>
                o.UseSqlite(fixture.Connection)
                    .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning)))
            .AddIdentity<IdentityUser, IdentityRole>(o =>
            {
                // Versions >= 3 are custom
                o.Stores.SchemaVersion = new Version(3, 0);
            })
            .AddEntityFrameworkStores<CustomVersionDbContext>();

        _builder = new ApplicationBuilder(services.BuildServiceProvider());
        using var scope = _builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomVersionDbContext>();
        db.Database.EnsureCreated();
    }

    [Fact]
    public void CanAddCustomColumn()
    {
        using var scope = _builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomVersionDbContext>();
        VersionTwoSchemaTest.VerifyVersion2Schema(db);
        using var sqlConn = (SqliteConnection)db.Database.GetDbConnection();
        sqlConn.Open();
        Assert.True(DbUtil.VerifyColumns(sqlConn, "CustomColumns", "Id"));
    }
}
