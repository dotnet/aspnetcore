// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class MaxKeyLengthSchemaTest : IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ApplicationBuilder _builder;

        public MaxKeyLengthSchemaTest(ScratchDatabaseFixture fixture)
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                .AddDbContext<VerstappenDbContext>(o =>
                    o.UseSqlite(fixture.Connection)
                        .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning)))
                .AddIdentity<IdentityUser, IdentityRole>(o => o.Stores.MaxLengthForKeys = 128)
                .AddEntityFrameworkStores<VerstappenDbContext>();

            services.AddLogging();

            _builder = new ApplicationBuilder(services.BuildServiceProvider());

            using (var scope = _builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<VerstappenDbContext>().Database.EnsureCreated();
            }
        }

        // Need a different context type here since EF model is changing by having MaxLengthForKeys
        public class VerstappenDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
        {
            public VerstappenDbContext(DbContextOptions options)
                : base(options)
                {
                }
        }

        [ConditionalFact]
        public void EnsureDefaultSchema()
        {
            using (var scope = _builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VerstappenDbContext>();

                VerifyDefaultSchema(db);
            }
        }

        private static void VerifyDefaultSchema(VerstappenDbContext dbContext)
        {
            var sqlConn = (SqliteConnection)dbContext.Database.GetDbConnection();

            try
            {
                sqlConn.Open();
                Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUsers", "Id", "UserName", "Email", "PasswordHash", "SecurityStamp",
                    "EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
                    "LockoutEnd", "AccessFailedCount", "ConcurrencyStamp", "NormalizedUserName", "NormalizedEmail"));
                Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetRoles", "Id", "Name", "NormalizedName", "ConcurrencyStamp"));
                Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserRoles", "UserId", "RoleId"));
                Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserClaims", "Id", "UserId", "ClaimType", "ClaimValue"));
                Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserLogins", "UserId", "ProviderKey", "LoginProvider", "ProviderDisplayName"));
                Assert.True(DbUtil.VerifyColumns(sqlConn, "AspNetUserTokens", "UserId", "LoginProvider", "Name", "Value"));

                Assert.True(DbUtil.VerifyMaxLength(dbContext, "AspNetUsers", 256, "UserName", "Email", "NormalizedUserName", "NormalizedEmail"));
                Assert.True(DbUtil.VerifyMaxLength(dbContext, "AspNetRoles", 256, "Name", "NormalizedName"));
                Assert.True(DbUtil.VerifyMaxLength(dbContext, "AspNetUserLogins", 128, "LoginProvider", "ProviderKey"));
                Assert.True(DbUtil.VerifyMaxLength(dbContext, "AspNetUserTokens", 128, "LoginProvider", "Name"));

                DbUtil.VerifyIndex(sqlConn, "AspNetRoles", "RoleNameIndex", isUnique: true);
                DbUtil.VerifyIndex(sqlConn, "AspNetUsers", "UserNameIndex", isUnique: true);
                DbUtil.VerifyIndex(sqlConn, "AspNetUsers", "EmailIndex");
            }
            finally
            {
                sqlConn.Close();
            }
        }
    }
}
