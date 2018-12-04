// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data.SqlClient;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class MaxKeyLengthSchemaTest : IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ApplicationBuilder _builder;
        private const string DatabaseName = nameof(MaxKeyLengthSchemaTest);

        public MaxKeyLengthSchemaTest(ScratchDatabaseFixture fixture)
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                .AddDbContext<IdentityDbContext>(o => o.UseSqlServer(fixture.ConnectionString))
                .AddIdentity<IdentityUser, IdentityRole>(o => o.Stores.MaxLengthForKeys = 128)
                .AddEntityFrameworkStores<IdentityDbContext>();

            services.AddLogging();

            var provider = services.BuildServiceProvider();
            _builder = new ApplicationBuilder(provider);

            using (var scoped = provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<IdentityDbContext>())
            {
                db.Database.EnsureCreated();
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void EnsureDefaultSchema()
        {
            var db = _builder.ApplicationServices.GetRequiredService<IdentityDbContext>();
            VerifyDefaultSchema(db);
        }

        private static void VerifyDefaultSchema(IdentityDbContext dbContext)
        {
            var sqlConn = dbContext.Database.GetDbConnection();

            using (var db = new SqlConnection(sqlConn.ConnectionString))
            {
                db.Open();
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUsers", "Id", "UserName", "Email", "PasswordHash", "SecurityStamp",
                    "EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
                    "LockoutEnd", "AccessFailedCount", "ConcurrencyStamp", "NormalizedUserName", "NormalizedEmail"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetRoles", "Id", "Name", "NormalizedName", "ConcurrencyStamp"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserRoles", "UserId", "RoleId"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserClaims", "Id", "UserId", "ClaimType", "ClaimValue"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserLogins", "UserId", "ProviderKey", "LoginProvider", "ProviderDisplayName"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserTokens", "UserId", "LoginProvider", "Name", "Value"));

                Assert.True(DbUtil.VerifyMaxLength(db, "AspNetUsers", 256, "UserName", "Email", "NormalizedUserName", "NormalizedEmail"));
                Assert.True(DbUtil.VerifyMaxLength(db, "AspNetRoles", 256, "Name", "NormalizedName"));
                Assert.True(DbUtil.VerifyMaxLength(db, "AspNetUserLogins", 128, "LoginProvider", "ProviderKey"));
                Assert.True(DbUtil.VerifyMaxLength(db, "AspNetUserTokens", 128, "LoginProvider", "Name"));

                DbUtil.VerifyIndex(db, "AspNetRoles", "RoleNameIndex", isUnique: true);
                DbUtil.VerifyIndex(db, "AspNetUsers", "UserNameIndex", isUnique: true);
                DbUtil.VerifyIndex(db, "AspNetUsers", "EmailIndex");
                db.Close();
            }
        }
    }
}