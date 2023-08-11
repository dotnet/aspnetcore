// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.Test;

class DataProtectionKeyContextImplementation : DbContext, IDataProtectionKeyContextService
{
    public DataProtectionKeyContextImplementation(DbContextOptions<DataProtectionKeyContextImplementation> options) : base(options) { }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
}
