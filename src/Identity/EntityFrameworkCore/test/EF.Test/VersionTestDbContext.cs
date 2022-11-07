// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

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
