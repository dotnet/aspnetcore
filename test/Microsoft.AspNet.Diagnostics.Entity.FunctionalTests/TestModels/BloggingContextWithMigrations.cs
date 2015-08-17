// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.SqlServer.Metadata;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class BloggingContextWithMigrations : BloggingContext
    {
        protected BloggingContextWithMigrations(DbContextOptions options)
            : base(options)
        { }

        public BloggingContextWithMigrations(IServiceProvider provider, DbContextOptions options)
            : base(provider, options)
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
                    b.Property<int>("BlogId");
                    b.Property<string>("Name");
                    b.Key("BlogId");
                });
            }
        }

        [DbContext(typeof(BloggingContextWithMigrations))]
        public class MigrationOne : Migration
        {
            public override string Id
            {
                get { return "111111111111111_MigrationOne"; }
            }

            public override IModel TargetModel => new BloggingContextWithMigrationsModelSnapshot().Model;

            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable("Blog",
                c => new
                {
                    BlogId = c.Column<int>().Annotation("SqlServer:ValueGenerationStrategy", SqlServerIdentityStrategy.IdentityColumn),
                    Name = c.Column<string>(isNullable: true),
                })
                .PrimaryKey("PK_Blog", t => t.BlogId);
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable("Blog");
            }
        }

        [DbContext(typeof(BloggingContextWithMigrations))]
        public class MigrationTwo : Migration
        {
            public override string Id
            {
                get { return "222222222222222_MigrationTwo"; }
            }

            public override IModel TargetModel => new BloggingContextWithMigrationsModelSnapshot().Model;

            protected override void Up(MigrationBuilder migrationBuilder)
            { }
        }
    }
}