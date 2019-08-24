// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration
{
    public class UserConfiguration : UserConfiguration<IdentityUser, IdentityUserClaim<string>, IdentityUserLogin<string>, IdentityUserToken<string>, string>
    {
    }

    public class UserConfiguration<TUser> : UserConfiguration<TUser, IdentityUserClaim<string>, IdentityUserLogin<string>, IdentityUserToken<string>, string>
        where TUser : IdentityUser<string>
    {
    }

    public class UserConfiguration<TUser, TUserClaim, TUserLogin, TUserToken, TKey> : IEntityTypeConfiguration<TUser>
         where TUser : IdentityUser<TKey>
         where TUserClaim : IdentityUserClaim<TKey>
         where TUserLogin : IdentityUserLogin<TKey>
         where TUserToken : IdentityUserToken<TKey>
         where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Specifies the maximum key length.
        /// </summary>
        /// <remarks>Only applied if greater than 0.</remarks>
        public int MaxKeyLength { get; set; } = 0;

        /// <summary>
        /// If set, all properties on type <typeparamref name="TUser"/> marked with a <see cref="ProtectedPersonalDataAttribute"/> will be converted using this <see cref="ValueConverter"/>.
        /// </summary>
        public ValueConverter<string, string> PersonalDataConverter { get; set; } = null;

        public virtual void Configure(EntityTypeBuilder<TUser> builder)
        {

            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.NormalizedUserName).HasName("UserNameIndex").IsUnique();
            builder.HasIndex(u => u.NormalizedEmail).HasName("EmailIndex");
            builder.ToTable("AspNetUsers");
            builder.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

            builder.Property(u => u.UserName).HasMaxLength(256);
            builder.Property(u => u.NormalizedUserName).HasMaxLength(256);
            builder.Property(u => u.Email).HasMaxLength(256);
            builder.Property(u => u.NormalizedEmail).HasMaxLength(256);

            if (PersonalDataConverter != null)
            {
                var personalDataProps = typeof(TUser).GetProperties().Where(
                                prop => Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)));
                foreach (var p in personalDataProps)
                {
                    if (p.PropertyType != typeof(string))
                    {
                        throw new InvalidOperationException(Resources.CanOnlyProtectStrings);
                    }
                    builder.Property(typeof(string), p.Name).HasConversion(PersonalDataConverter);
                }
            }

            builder.HasMany<TUserClaim>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
            builder.HasMany<TUserLogin>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
            builder.HasMany<TUserToken>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();

        }
    }
}
