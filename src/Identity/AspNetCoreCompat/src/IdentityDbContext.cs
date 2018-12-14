// Copyright (c) Microsoft Corporation, Inc. All rights reserved.
// Licensed under the MIT License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Microsoft.AspNet.Identity.CoreCompat
{
    public class IdentityDbContext<TUser> :
        IdentityDbContext<TUser, IdentityRole, string,
            IdentityUserLogin, IdentityUserRole, IdentityUserClaim, IdentityRoleClaim>
        where TUser : IdentityUser
    {
        public IdentityDbContext() : base()
        {

        }

        public IdentityDbContext(DbCompiledModel model)
            : base(model)
        {

        }

        public IdentityDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {

        }

        public IdentityDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {

        }

        public IdentityDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {

        }
        //
        // Summary:
        //     Constructs a new context instance using the existing connection to connect to
        //     a database, and initializes it from the given model. The connection will not
        //     be disposed when the context is disposed if contextOwnsConnection is false.
        //
        // Parameters:
        //   existingConnection:
        //     An existing connection to use for the new context.
        //
        //   model:
        //     The model that will back this context.
        //
        //   contextOwnsConnection:
        //     Constructs a new context instance using the existing connection to connect to
        //     a database, and initializes it from the given model. The connection will not
        //     be disposed when the context is disposed if contextOwnsConnection is false.
        public IdentityDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
        }
    }

    public class IdentityDbContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim, TRoleClaim> :
        IdentityDbContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim>
        where TUser : IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>
        where TRole : IdentityRole<TKey, TUserRole>
        where TUserLogin : IdentityUserLogin<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
    {

        public IdentityDbContext() : base()
        {
        }

        public IdentityDbContext(DbCompiledModel model)
            : base(model)
        {
        }

        public IdentityDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public IdentityDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
        }

        public IdentityDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
        }

        public IdentityDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var userModel = modelBuilder.Entity<TUser>();

            userModel.Property(x => x.LockoutEndDateUtc).HasColumnName("LockoutEnd");
            userModel.Property(x => x.ConcurrencyStamp).IsConcurrencyToken(true);

            modelBuilder.Entity<TRoleClaim>()
                .HasKey(x => x.Id)
                .Map(config => config.ToTable("AspNetRoleClaims"));

            modelBuilder.Entity<TRole>().Property(x => x.ConcurrencyStamp).IsConcurrencyToken(true);
        }

        protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry, IDictionary<object, object> items)
        {
            if (entityEntry.Entity is TUser && entityEntry.State == EntityState.Modified)
            {
                entityEntry.Cast<TUser>().Property(p => p.ConcurrencyStamp).CurrentValue = Guid.NewGuid().ToString();
            }
            else if (entityEntry.Entity is TRole && entityEntry.State == EntityState.Modified)
            {
                entityEntry.Cast<TRole>().Property(p => p.ConcurrencyStamp).CurrentValue = Guid.NewGuid().ToString();
            }
            return base.ValidateEntity(entityEntry, items);
        }

        public virtual IDbSet<TRoleClaim> RoleClaims { get; set; }
    }
}
