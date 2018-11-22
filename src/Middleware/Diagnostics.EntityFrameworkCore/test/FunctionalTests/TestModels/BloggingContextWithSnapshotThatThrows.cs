// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests
{
    public class BloggingContextWithSnapshotThatThrows : BloggingContext
    {
        public BloggingContextWithSnapshotThatThrows(DbContextOptions options)
            : base(options)
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