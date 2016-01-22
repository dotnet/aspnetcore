// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class BloggingContextWithSnapshotThatThrows : BloggingContext
    {
        public BloggingContextWithSnapshotThatThrows(IServiceProvider provider, DbContextOptions options)
            : base(provider, options)
        { }

        [DbContext(typeof(BloggingContextWithSnapshotThatThrows))]
        public class BloggingContextWithSnapshotThatThrowsModelSnapshot : ModelSnapshot
        {
            protected override void BuildModel(ModelBuilder modelBuilder)
            {
                throw new Exception("Welcome to the invalid snapshot!");
            }
        }

        [DbContext(typeof(BloggingContextWithSnapshotThatThrows))]
        [Migration("111111111111111_MigrationOne")]
        public class MigrationOne : Migration
        {
            public override IModel TargetModel => new BloggingContextWithSnapshotThatThrowsModelSnapshot().Model;

            protected override void Up(MigrationBuilder migrationBuilder)
            {
                throw new Exception("Welcome to the invalid migration!");
            }
        }
    }
}