// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace Microsoft.AspNet.Identity.EntityFramework
{
    public class IdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, string> { }

    public class IdentityDbContext<TUser> : IdentityDbContext<TUser, IdentityRole, string> where TUser : IdentityUser
    { }

    public class IdentityDbContext<TUser, TRole, TKey> : DbContext
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        public DbSet<TUser> Users { get; set; }
        public DbSet<IdentityUserClaim<TKey>> UserClaims { get; set; }
        public DbSet<IdentityUserLogin<TKey>> UserLogins { get; set; }
        public DbSet<IdentityUserRole<TKey>> UserRoles { get; set; }
        public DbSet<TRole> Roles { get; set; }
        public DbSet<IdentityRoleClaim<TKey>> RoleClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<TUser>(b =>
                {
                    b.Key(u => u.Id);
                    b.ForRelational().Table("AspNetUsers");
                });

            builder.Entity<TRole>(b =>
                {
                    b.Key(r => r.Id);
                    b.ForRelational().Table("AspNetRoles");
                });

            builder.Entity<IdentityUserClaim<TKey>>(b =>
                {
                    b.Key(uc => uc.Id);
                    b.ManyToOne<TUser>().ForeignKey(uc => uc.UserId);
                    b.ForRelational().Table("AspNetUserClaims");
                });

            builder.Entity<IdentityRoleClaim<TKey>>(b =>
                {
                    b.Key(rc => rc.Id);
                    b.ManyToOne<TRole>().ForeignKey(rc => rc.RoleId);
                    b.ForRelational().Table("AspNetRoleClaims");
                });

            var userType = builder.Model.GetEntityType(typeof(TUser));
            var roleType = builder.Model.GetEntityType(typeof(TRole));
            var userClaimType = builder.Model.GetEntityType(typeof(IdentityUserClaim<TKey>));
            var roleClaimType = builder.Model.GetEntityType(typeof(IdentityRoleClaim<TKey>));
            var userRoleType = builder.Model.GetEntityType(typeof(IdentityUserRole<TKey>));
            //var ucfk = userClaimType.GetOrAddForeignKey(userType.GetPrimaryKey(), new[] { userClaimType.GetProperty("UserId") });
            //userType.AddNavigation(new Navigation(ucfk, "Claims", false));
            //userClaimType.AddNavigation(new Navigation(ucfk, "User", true));
            //var urfk = userRoleType.GetOrAddForeignKey(userType.GetPrimaryKey(), new[] { userRoleType.GetProperty("UserId") });
            //userType.AddNavigation(new Navigation(urfk, "Roles", false));

            //var urfk2 = userRoleType.GetOrAddForeignKey(roleType.GetPrimaryKey(), new[] { userRoleType.GetProperty("RoleId") });
            //roleType.AddNavigation(new Navigation(urfk2, "Users", false));

            var rcfk = roleClaimType.GetOrAddForeignKey(new[] { roleClaimType.GetProperty("RoleId") }, roleType.GetPrimaryKey());
            roleType.AddNavigation("Claims", rcfk, false);

            builder.Entity<IdentityUserRole<TKey>>(b =>
                {
                    b.Key(r => new { r.UserId, r.RoleId });
                    b.ForRelational().Table("AspNetUserRoles");
                });
            // Blocks delete currently without cascade
            //.ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId))
            //.ForeignKeys(fk => fk.ForeignKey<TRole>(f => f.RoleId));

            builder.Entity<IdentityUserLogin<TKey>>(b =>
                {
                    b.Key(l => new { l.LoginProvider, l.ProviderKey });
                    b.ManyToOne<TUser>().ForeignKey(uc => uc.UserId);
                    b.ForRelational().Table("AspNetUserLogins");
                });
        }
    }
}