// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.SqlServer;
using Microsoft.Data.Entity.InMemory;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Identity.Entity
{
    public class IdentityContext :
        IdentityContext<EntityUser, EntityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
    {
        public IdentityContext() { }
        public IdentityContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public class IdentityContext<TUser> :
        IdentityContext<TUser, EntityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
        where TUser : EntityUser<string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
    {
        public IdentityContext() { }
        public IdentityContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public class IdentityContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim> : DbContext
        where TUser : EntityUser<TKey, TUserLogin, TUserRole, TUserClaim>
        where TRole : EntityRole<TKey, TUserRole>
        where TUserLogin : IdentityUserLogin<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TKey : IEquatable<TKey>
    {

        public DbSet<TUser> Users { get; set; }
        public DbSet<TRole> Roles { get; set; }

        public IdentityContext(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

        public IdentityContext() { }

        protected override void OnConfiguring(DbContextOptions builder)
        {
//#if NET45
//            builder.SqlServerConnectionString(@"Server=(localdb)\v11.0;Database=IdentityEF5-5-1;Trusted_Connection=True;");
//#else
            builder.UseInMemoryStore();
//#endif
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<TUser>()
                .Key(u => u.Id)
                .Properties(ps => ps.Property(u => u.UserName));
                //.ToTable("AspNetUsers");
            builder.Entity<TRole>()
                .Key(r => r.Id);
                //.ToTable("AspNetRoles");
 
            builder.Entity<TUserRole>()
                .Key(u => u.Id)
                //TODO: .Key(r => new { r.UserId, r.RoleId })
                .ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId))
                .ForeignKeys(fk => fk.ForeignKey<TRole>(f => f.RoleId));
                //.ToTable("AspNetUserRoles");

            builder.Entity<TUserLogin>()
                .Key(u => u.Id)
                //TODO: .Key(l => new { l.LoginProvider, l.ProviderKey, l.UserId })
                .ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId));
            //.ToTable("AspNetUserLogins");

            builder.Entity<TUserClaim>()
                .Key(c => c.Id)
                .ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId));
            //.ToTable("AspNetUserClaims");

        }

    }
}