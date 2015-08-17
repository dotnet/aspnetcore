// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Migrations;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class BloggingContextWithPendingModelChanges : BloggingContext
    {
        public BloggingContextWithPendingModelChanges(IServiceProvider provider, DbContextOptions options)
            : base(provider, options)
        { }

        [DbContext(typeof(BloggingContextWithPendingModelChanges))]
        public class BloggingModelSnapshot : ModelSnapshot
        {
            protected override void BuildModel(ModelBuilder modelBuilder)
            {
            }
        }

        [DbContext(typeof(BloggingContextWithPendingModelChanges))]
        public partial class MigrationOne : Migration
        {
            public override string Id
            {
                get { return "111111111111111_MigrationOne"; }
            }

            protected override void Up(MigrationBuilder migrationBuilder)
            { }
        }
    }
}