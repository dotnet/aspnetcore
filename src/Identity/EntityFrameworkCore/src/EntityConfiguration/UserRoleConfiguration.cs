// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration
{
    public class UserRoleConfiguration<TUserRole, TKey> : IEntityTypeConfiguration<TUserRole>      
         where TUserRole : IdentityUserRole<TKey>
         where TKey : IEquatable<TKey>
    {
        public virtual void Configure(EntityTypeBuilder<TUserRole> builder)
        {
            builder.HasKey(r => new { r.UserId, r.RoleId });
            builder.ToTable("AspNetUserRoles");
        }
    }
}
