// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration
{
    public class UserLoginConfiguration : UserLoginConfiguration<IdentityUserLogin<string>, string>
    {
    }

    public class UserLoginConfiguration<TUserLogin> : UserLoginConfiguration<TUserLogin, string>
        where TUserLogin : IdentityUserLogin<string>
    {
    }

    public class UserLoginConfiguration<TUserLogin, TKey> : IEntityTypeConfiguration<TUserLogin>
    where TUserLogin : IdentityUserLogin<TKey>
    where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Specifies the maximum key length.
        /// </summary>
        /// <remarks>Only applied if greater than 0.</remarks>
        public int MaxKeyLength { get; set; } = 0;

        public virtual void Configure(EntityTypeBuilder<TUserLogin> builder)
        {
            builder.HasKey(l => new { l.LoginProvider, l.ProviderKey });

            if (MaxKeyLength > 0)
            {
                builder.Property(l => l.LoginProvider).HasMaxLength(MaxKeyLength);
                builder.Property(l => l.ProviderKey).HasMaxLength(MaxKeyLength);
            }

            builder.ToTable("AspNetUserLogins");
        }
    }
}
