// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests
{
    public class BloggingContextWithMigrations : BloggingContext
    {
        public BloggingContextWithMigrations(DbContextOptions options)
            : base(options)
        { }

        // Providing a factory method so that the ctor is hidden from DI
        public static BloggingContextWithMigrations CreateWithoutExternalServiceProvider(DbContextOptions options)
        {
            return new BloggingContextWithMigrations(options);
        }

        [DbContext(typeof(BloggingContextWithMigrations))]
        public class BloggingContextWithMigrationsModelSnapshot : ModelSnapshot
        {
            protected override void BuildModel(ModelBuilder builder)
            {
                builder.Entity("Blogging.Models.Blog", b =>
                {
                    b.Property<int>("BlogId").ValueGeneratedOnAdd();
                    b.Property<string>("Name");
                    b.HasKey("BlogId");
                });
            }
        }

        [DbContext(typeof(BloggingContextWithMigrations))]
        [Migration("111111111111111_MigrationOne")]
        public class MigrationOne : Migration
        {
            public override IModel TargetModel => new BloggingContextWithMigrationsModelSnapshot().Model;

            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable("Blogs",
                c => new
                {
                    BlogId = c.Column<int>().Annotation("Sqlite:Autoincrement", true),
                    Name = c.Column<string>(nullable: true),
                })
                .PrimaryKey("PK_Blog", t => t.BlogId);
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable("Blogs");
            }
        }

        [DbContext(typeof(BloggingContextWithMigrations))]
        [Migration("222222222222222_MigrationTwo")]
        public class MigrationTwo : Migration
        {
            public override IModel TargetModel => new BloggingContextWithMigrationsModelSnapshot().Model;

            protected override void Up(MigrationBuilder migrationBuilder)
            { }
        }
    }
}
