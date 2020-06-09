// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class UserOnlyCustomContextTest : IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ApplicationBuilder _builder;

        public class CustomContext : DbContext
        {
            public CustomContext(DbContextOptions options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<IdentityUser>(b =>
                {
                    b.HasKey(u => u.Id);
                    b.HasIndex(u => u.NormalizedUserName).HasName("UserNameIndex").IsUnique();
                    b.HasIndex(u => u.NormalizedEmail).HasName("EmailIndex");
                    b.ToTable("AspNetUsers");
                    b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

                    b.Property(u => u.UserName).HasMaxLength(256);
                    b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                    b.Property(u => u.Email).HasMaxLength(256);
                    b.Property(u => u.NormalizedEmail).HasMaxLength(256);

                    b.HasMany<IdentityUserClaim<string>>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
                    b.HasMany<IdentityUserLogin<string>>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
                    b.HasMany<IdentityUserToken<string>>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
                });

                builder.Entity<IdentityUserClaim<string>>(b =>
                {
                    b.HasKey(uc => uc.Id);
                    b.ToTable("AspNetUserClaims");
                });

                builder.Entity<IdentityUserLogin<string>>(b =>
                {
                    b.HasKey(l => new { l.LoginProvider, l.ProviderKey });
                    b.ToTable("AspNetUserLogins");
                });

                builder.Entity<IdentityUserToken<string>>(b =>
                {
                    b.HasKey(l => new { l.UserId, l.LoginProvider, l.Name });
                    b.ToTable("AspNetUserTokens");
                });
            }
        }

        public UserOnlyCustomContextTest(ScratchDatabaseFixture fixture)
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                .AddDbContext<CustomContext>(o =>
                    o.UseSqlite(fixture.Connection)
                        .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning)))
                .AddIdentityCore<IdentityUser>(o => { })
                .AddEntityFrameworkStores<CustomContext>();

            services.AddLogging();

            var provider = services.BuildServiceProvider();
            _builder = new ApplicationBuilder(provider);

            using (var scoped = provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<CustomContext>())
            {
                db.Database.EnsureCreated();
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
            const string password = "1qaz@WSX";
            var user = new IdentityUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
        }

    }
}
