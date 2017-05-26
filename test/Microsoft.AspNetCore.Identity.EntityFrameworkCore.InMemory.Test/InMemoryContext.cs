// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test
{
    public class InMemoryContext :
        InMemoryContext<IdentityUser, IdentityRole, string>
    {
        public InMemoryContext(DbContextOptions options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("Scratch");
    }

    public class InMemoryContext<TUser> :
        InMemoryContext<TUser, IdentityRole, string>
        where TUser : IdentityUser
    {
        public InMemoryContext(DbContextOptions options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("Scratch");
    }

    public class InMemoryContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        public InMemoryContext(DbContextOptions options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("Scratch");
    }

    public abstract class InMemoryContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
        where TUserToken : IdentityUserToken<TKey>
    {
        public InMemoryContext(DbContextOptions options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("Scratch");
    }

    public class InMemoryContextV1 :
        InMemoryContextV1<IdentityUser, IdentityRole, string>
    {
        public InMemoryContextV1(DbContextOptions options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("ScratchV1");
    }

    public class InMemoryContextV1<TUser> :
        InMemoryContext<TUser, IdentityRole, string>
        where TUser : IdentityUser
    {
        public InMemoryContextV1(DbContextOptions options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("ScratchV1");
    }

    public class InMemoryContextV1<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        public InMemoryContextV1(DbContextOptions options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("ScratchV1");
    }

    public abstract class InMemoryContextV1<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
        where TUserToken : IdentityUserToken<TKey>
    {
        public InMemoryContextV1(DbContextOptions options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("ScratchV1");

        // TODO: remove once we fix the base
        protected override void OnModelCreating(ModelBuilder builder)
            => base.OnModelCreatingV1(builder);
    }
}