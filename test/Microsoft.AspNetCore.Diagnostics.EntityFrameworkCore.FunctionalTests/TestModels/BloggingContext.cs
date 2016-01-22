// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class BloggingContext : DbContext
    {
        protected BloggingContext(DbContextOptions options)
            : base(options)
        { }

        public BloggingContext(IServiceProvider provider, DbContextOptions options)
            : base(provider, options)
        { }

        public DbSet<Blog> Blogs { get; set; }
    }
}