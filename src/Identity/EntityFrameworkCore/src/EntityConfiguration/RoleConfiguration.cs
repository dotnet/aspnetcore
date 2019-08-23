// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration
{
    public class RoleConfiguration<TRole, TUserRole, TRoleClaim, TKey> : IEntityTypeConfiguration<TRole>
         where TRole : IdentityRole<TKey>
         where TUserRole : IdentityUserRole<TKey>
         where TRoleClaim : IdentityRoleClaim<TKey>
         where TKey : IEquatable<TKey>
    {
        public virtual void Configure(EntityTypeBuilder<TRole> builder)
        {
            builder.HasKey(r => r.Id);
            builder.HasIndex(r => r.NormalizedName).HasName("RoleNameIndex").IsUnique();
            builder.ToTable("AspNetRoles");
            builder.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

            builder.Property(u => u.Name).HasMaxLength(256);
            builder.Property(u => u.NormalizedName).HasMaxLength(256);

            builder.HasMany<TUserRole>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
            builder.HasMany<TRoleClaim>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
        }
    }
}
