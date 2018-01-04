// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore.InMemory.Test
{
    public class InMemoryContext : InMemoryContext<IdentityUser, IdentityServiceApplication>
    {
        public InMemoryContext(DbContextOptions options) : base(options)
        { }
    }

    public class InMemoryContext<TUser, TApplication> :
        InMemoryContext<TUser, IdentityRole, string, TApplication, string>
        where TUser : IdentityUser
        where TApplication : IdentityServiceApplication
    {
        public InMemoryContext(DbContextOptions options) : base(options)
        { }
    }

    public class InMemoryContext<TUser, TRole, TUserKey, TApplication, TApplicationKey> : IdentityServiceDbContext<TUser, TRole, TUserKey, TApplication, TApplicationKey>
        where TUser : IdentityUser<TUserKey>
        where TRole : IdentityRole<TUserKey>
        where TUserKey : IEquatable<TUserKey>
        where TApplication : IdentityServiceApplication<TApplicationKey, TUserKey>
        where TApplicationKey : IEquatable<TApplicationKey>
    {
        public InMemoryContext(DbContextOptions options) : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("Scratch");
        }
    }

    public abstract class InMemoryContext<
        TUser,
        TRole,
        TUserKey,
        TUserClaim,
        TUserRole,
        TUserLogin,
        TRoleClaim,
        TUserToken,
        TApplication,
        TScope,
        TApplicationClaim,
        TRedirectUri,
        TApplicationKey> :
        IdentityServiceDbContext<TUser, TRole, TUserKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken, TApplication, TScope, TApplicationClaim, TRedirectUri, TApplicationKey>
        where TUser : IdentityUser<TUserKey>
        where TRole : IdentityRole<TUserKey>
        where TUserKey : IEquatable<TUserKey>
        where TUserClaim : IdentityUserClaim<TUserKey>
        where TUserRole : IdentityUserRole<TUserKey>
        where TUserLogin : IdentityUserLogin<TUserKey>
        where TRoleClaim : IdentityRoleClaim<TUserKey>
        where TUserToken : IdentityUserToken<TUserKey>
        where TApplication : IdentityServiceApplication<TApplicationKey, TUserKey, TScope, TApplicationClaim, TRedirectUri>
        where TScope : IdentityServiceScope<TApplicationKey>
        where TApplicationClaim : IdentityServiceApplicationClaim<TApplicationKey>
        where TRedirectUri : IdentityServiceRedirectUri<TApplicationKey>
        where TApplicationKey : IEquatable<TApplicationKey>
    {
        public InMemoryContext(DbContextOptions options) : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("Scratch");
        }
    }
}
