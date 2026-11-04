// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests;

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
