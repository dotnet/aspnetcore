// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration
{
    public class UserClaimConfiguration : UserClaimConfiguration<IdentityUserClaim<string>>
    {
    }

    public class UserClaimConfiguration<TUserClaim> : UserClaimConfiguration<TUserClaim, string>
       where TUserClaim : IdentityUserClaim<string>
    {
    }

    public class UserClaimConfiguration<TUserClaim, TKey> : IEntityTypeConfiguration<TUserClaim>
         where TUserClaim : IdentityUserClaim<TKey>
         where TKey : IEquatable<TKey>
    {
        public virtual void Configure(EntityTypeBuilder<TUserClaim> builder)
        {
            builder.HasKey(uc => uc.Id);
            builder.ToTable("AspNetUserClaims");
        }
    }
}
