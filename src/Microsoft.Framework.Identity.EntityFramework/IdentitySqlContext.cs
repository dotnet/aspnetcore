// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace Microsoft.AspNet.Identity.Entity
{
    public class IdentitySqlContext :
        IdentitySqlContext<User>
    {
        public IdentitySqlContext() { }
        public IdentitySqlContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
        public IdentitySqlContext(IServiceProvider serviceProvider, string nameOrConnectionString) : base(serviceProvider, nameOrConnectionString) { }
        public IdentitySqlContext(DbContextOptions options) : base(options) { }
        public IdentitySqlContext(IServiceProvider serviceProvider, DbContextOptions options) : base(serviceProvider, options) { }
    }

    public class IdentitySqlContext<TUser> : DbContext
        where TUser : User
    {
        public DbSet<TUser> Users { get; set; }
        public DbSet<IdentityUserClaim> UserClaims { get; set; }
        //public DbSet<TRole> Roles { get; set; }

        private readonly string _nameOrConnectionString;

        public IdentitySqlContext() { }
        public IdentitySqlContext(IServiceProvider serviceProvider, string nameOrConnectionString) : base(serviceProvider)
        {
            _nameOrConnectionString = nameOrConnectionString;
        }
        public IdentitySqlContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
        public IdentitySqlContext(DbContextOptions options) : base(options) { }
        public IdentitySqlContext(IServiceProvider serviceProvider, DbContextOptions options) : base(serviceProvider, options) { }

        protected override void OnConfiguring(DbContextOptions builder)
        {
            if (!string.IsNullOrEmpty(_nameOrConnectionString))
            {
                builder.UseSqlServer(_nameOrConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<TUser>()
                .Key(u => u.Id)
                .Properties(ps => ps.Property(u => u.UserName))
                .ToTable("AspNetUsers");

            builder.Entity<IdentityUserClaim>()
                .Key(uc => uc.Id)
                // TODO: this throws a length exception currently, investigate
                //.ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId))
                .ToTable("AspNetUserClaims");
        }
    }
}