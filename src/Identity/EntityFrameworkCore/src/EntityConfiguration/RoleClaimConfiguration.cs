// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration
{
    public class RoleClaimConfiguration<TRoleClaim, TKey> : IEntityTypeConfiguration<TRoleClaim>      
         where TRoleClaim : IdentityRoleClaim<TKey>
         where TKey : IEquatable<TKey>
    {
        public virtual void Configure(EntityTypeBuilder<TRoleClaim> builder)
        {         
                builder.HasKey(rc => rc.Id);
                builder.ToTable("AspNetRoleClaims");            
        }
    }
}
