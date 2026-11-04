// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.Test;

class DataProtectionKeyContext : DbContext, IDataProtectionKeyContext
{
    public DataProtectionKeyContext(DbContextOptions<DataProtectionKeyContext> options) : base(options) { }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
}
