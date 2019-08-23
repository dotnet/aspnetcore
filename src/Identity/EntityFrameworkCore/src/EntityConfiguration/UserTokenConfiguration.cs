// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration
{
    public class UserTokenConfiguration<TUserToken, TKey> : IEntityTypeConfiguration<TUserToken>
       where TUserToken : IdentityUserToken<TKey>
       where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Specifies the maximum key length.
        /// </summary>
        /// <remarks>Only applied if greater than 0.</remarks>
        public int MaxKeyLength { get; set; } = 0;

        /// <summary>
        /// If set, all properties on type <typeparamref name="TUserToken"/> marked with a <see cref="ProtectedPersonalDataAttribute"/> will be converted using this <see cref="ValueConverter"/>.
        /// </summary>
        public ValueConverter<string, string> PersonalDataConverter { get; set; } = null;

        public virtual void Configure(EntityTypeBuilder<TUserToken> builder)
        {
            builder.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            if (MaxKeyLength > 0)
            {
                builder.Property(t => t.LoginProvider).HasMaxLength(MaxKeyLength);
                builder.Property(t => t.Name).HasMaxLength(MaxKeyLength);
            }

            if (PersonalDataConverter != null)
            {
                var tokenProps = typeof(TUserToken).GetProperties().Where(
                                prop => Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)));
                foreach (var p in tokenProps)
                {
                    if (p.PropertyType != typeof(string))
                    {
                        throw new InvalidOperationException(Resources.CanOnlyProtectStrings);
                    }
                    builder.Property(typeof(string), p.Name).HasConversion(PersonalDataConverter);
                }
            }

            builder.ToTable("AspNetUserTokens");          
        }
    }
}
