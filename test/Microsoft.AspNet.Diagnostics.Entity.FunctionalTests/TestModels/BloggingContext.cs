// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Data.Entity.SqlServer.FunctionalTests;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class BloggingContext : DbContext
    {
        protected static readonly string CurrentProductVersion = typeof(HistoryRepository)
            .GetTypeInfo()
            .Assembly
            .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
            .Single()
            .InformationalVersion;

        protected BloggingContext(DbContextOptions options)
            : base(options)
        { }

        public BloggingContext(IServiceProvider provider, DbContextOptions options)
            : base(provider, options)
        { }

        public DbSet<Blog> Blogs { get; set; }
    }
}