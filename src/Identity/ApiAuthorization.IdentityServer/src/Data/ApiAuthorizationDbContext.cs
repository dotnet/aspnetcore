// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Extensions;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    /// <summary>
    /// Database abstraction for a combined <see cref="DbContext"/> using ASP.NET Identity and Identity Server.
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class ApiAuthorizationDbContext<TUser> : IdentityDbContext<TUser>, IPersistedGrantDbContext where TUser : IdentityUser
    {
        private readonly IOptions<OperationalStoreOptions> _operationalStoreOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="ApiAuthorizationDbContext{TUser}"/>.
        /// </summary>
        /// <param name="options">The <see cref="DbContextOptions"/>.</param>
        /// <param name="operationalStoreOptions">The <see cref="IOptions{OperationalStoreOptions}"/>.</param>
        public ApiAuthorizationDbContext(
            DbContextOptions options,
            IOptions<OperationalStoreOptions> operationalStoreOptions)
            : base(options)
        {
            _operationalStoreOptions = operationalStoreOptions;
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{PersistedGrant}"/>.
        /// </summary>
        public DbSet<PersistedGrant> PersistedGrants { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{DeviceFlowCodes}"/>.
        /// </summary>
        public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

        Task<int> IPersistedGrantDbContext.SaveChangesAsync() => base.SaveChangesAsync();

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            ConfigureGrantContext(builder, _operationalStoreOptions.Value);
        }

        private void ConfigureGrantContext(ModelBuilder modelBuilder, OperationalStoreOptions storeOptions)
        {
                if (!string.IsNullOrWhiteSpace(storeOptions.DefaultSchema)) modelBuilder.HasDefaultSchema(storeOptions.DefaultSchema);

                modelBuilder.Entity<PersistedGrant>(grant =>
                {
                    grant.ToTable("PersistedGrants");

                    grant.Property(x => x.Key).HasMaxLength(200).ValueGeneratedNever();
                    grant.Property(x => x.Type).HasMaxLength(50).IsRequired();
                    grant.Property(x => x.SubjectId).HasMaxLength(200);
                    grant.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
                    grant.Property(x => x.CreationTime).IsRequired();
                    grant.Property(x => x.Data).HasMaxLength(50000).IsRequired();

                    grant.HasKey(x => x.Key);

                    grant.HasIndex(x => new { x.SubjectId, x.ClientId, x.Type });
                });

                modelBuilder.Entity<DeviceFlowCodes>(codes =>
                {
                    codes.ToTable("DeviceCodes");

                    codes.Property(x => x.DeviceCode).HasMaxLength(200).IsRequired();
                    codes.Property(x => x.UserCode).HasMaxLength(200).IsRequired();
                    codes.Property(x => x.SubjectId).HasMaxLength(200);
                    codes.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
                    codes.Property(x => x.CreationTime).IsRequired();
                    codes.Property(x => x.Expiration).IsRequired();
                    codes.Property(x => x.Data).HasMaxLength(50000).IsRequired();

                    codes.HasKey(x => new { x.UserCode });

                    codes.HasIndex(x => x.DeviceCode).IsUnique();
                    codes.HasIndex(x => x.UserCode).IsUnique();
                });
            }
    }
}
