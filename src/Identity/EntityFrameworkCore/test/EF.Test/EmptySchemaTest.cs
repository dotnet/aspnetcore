// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public class EmptySchemaTest : IClassFixture<ScratchDatabaseFixture>
{
    private readonly ApplicationBuilder _builder;

    public EmptySchemaTest(ScratchDatabaseFixture fixture)
    {
        var services = new ServiceCollection();

        services
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddDbContext<EmptyDbContext>(o =>
                o.UseSqlite(fixture.Connection)
                    .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning)))
            .AddIdentity<IdentityUser, IdentityRole>(o =>
            {
                // Versions >= 10 are empty
                o.Stores.SchemaVersion = new Version(11, 0);
            })
            .AddEntityFrameworkStores<EmptyDbContext>();

        services.AddLogging();

        _builder = new ApplicationBuilder(services.BuildServiceProvider());

        using (var scope = _builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EmptyDbContext>();
            db.Database.EnsureCreated();
            Assert.False(db.OnModelCreatingVersion2Called);
            Assert.False(db.OnModelCreatingVersion1Called);
        }
    }

    [Fact]
    public void CanIgnoreEverything()
    {
        using (var scope = _builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EmptyDbContext>();

            VerifyEmptySchema(db);
        }
    }

    private static void VerifyEmptySchema(EmptyDbContext dbContext)
    {
        var sqlConn = (SqliteConnection)dbContext.Database.GetDbConnection();

        try
        {
            sqlConn.Open();
            Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUsers"));
            Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetRoles"));
            Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserRoles"));
            Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserClaims"));
            Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserLogins"));
            Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserTokens"));
        }
        finally
        {
            sqlConn.Close();
        }
    }
}

