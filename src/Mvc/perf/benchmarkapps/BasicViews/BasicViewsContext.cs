// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace BasicViews
{
    public class BasicViewsContext : DbContext
    {
        public BasicViewsContext(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Person> People { get; set; }
    }
}
