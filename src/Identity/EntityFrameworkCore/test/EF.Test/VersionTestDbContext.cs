// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

// Need a different context type here since EF model is changing by having MaxLengthForKeys
public class VersionOneDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public VersionOneDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public bool OnModelCreatingVersion1Called = false;
    public bool OnModelCreatingVersion2Called = false;

    protected override void OnModelCreatingVersion1(ModelBuilder builder)
    {
        base.OnModelCreatingVersion1(builder);
        OnModelCreatingVersion1Called = true;
    }

    protected override void OnModelCreatingVersion2(ModelBuilder builder)
    {
        base.OnModelCreatingVersion2(builder);
        OnModelCreatingVersion2Called = true;
    }
}

public class VersionTwoDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public VersionTwoDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public bool OnModelCreatingVersion1Called = false;
    public bool OnModelCreatingVersion2Called = false;

    protected override void OnModelCreatingVersion1(ModelBuilder builder)
    {
        base.OnModelCreatingVersion1(builder);
        OnModelCreatingVersion1Called = true;
    }

    protected override void OnModelCreatingVersion2(ModelBuilder builder)
    {
        base.OnModelCreatingVersion2(builder);
        OnModelCreatingVersion2Called = true;
    }
}

public class EmptyDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public EmptyDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public bool OnModelCreatingVersion1Called = false;
    public bool OnModelCreatingVersion2Called = false;

    protected override void OnModelCreatingVersion(ModelBuilder builder, Version schemaVersion)
    {
        if (schemaVersion >= new Version(10, 0))
        {
            builder.Ignore<IdentityUser>();

            builder.Ignore<IdentityUserClaim<string>>();

            builder.Ignore<IdentityUserLogin<string>>();

            builder.Ignore<IdentityUserToken<string>>();

            builder.Ignore<IdentityRole>();

            builder.Ignore<IdentityRoleClaim<string>>();

            builder.Ignore<IdentityUserRole<string>>();

        }
        else
        {
            base.OnModelCreatingVersion(builder, schemaVersion);
        }
    }

    protected override void OnModelCreatingVersion1(ModelBuilder builder)
    {
        base.OnModelCreatingVersion1(builder);
        OnModelCreatingVersion1Called = true;
    }

    protected override void OnModelCreatingVersion2(ModelBuilder builder)
    {
        base.OnModelCreatingVersion2(builder);
        OnModelCreatingVersion2Called = true;
    }
}

public class CustomColumn
{
    public string Id { get; set; }
}

public class CustomVersionDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public CustomVersionDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<CustomColumn> CustomColumns { get; set; }

    protected override void OnModelCreatingVersion(ModelBuilder builder, Version schemaVersion)
    {
        if (schemaVersion >= new Version(3, 0))
        {
            base.OnModelCreatingVersion2(builder);

            builder.Entity<CustomColumn>(b =>
            {
                b.HasKey(b => b.Id);
            });
        }
        else
        {
            base.OnModelCreatingVersion(builder, schemaVersion);
        }
    }
}
