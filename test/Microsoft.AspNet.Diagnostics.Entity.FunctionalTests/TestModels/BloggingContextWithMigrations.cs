// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational.Migrations;
using Microsoft.Data.Entity.Relational.Migrations.Builders;
using Microsoft.Data.Entity.Relational.Migrations.Infrastructure;
using System;
using System.Collections.Generic;

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

        [ContextType(typeof(BloggingContextWithMigrations))]
        public class BloggingContextWithMigrationsModelSnapshot : ModelSnapshot
        {
            public override IModel Model
            {
                get
                {
                    var builder = new BasicModelBuilder();

                    builder.Entity("Blogging.Models.Blog", b =>
                    {
                        b.Property<int>("BlogId");
                        b.Property<int>("BlogId").GenerateValueOnAdd();
                        b.Property<string>("Name");
                        b.Key("BlogId");
                    });

                    return builder.Model;
                }
            }
        }

        [ContextType(typeof(BloggingContextWithMigrations))]
        public class MigrationOne : Migration
        {
            public override string Id
            {
                get { return "111111111111111_MigrationOne"; }
            }

            public override string ProductVersion
            {
                get { return CurrentProductVersion; }
            }

            public override IModel Target
            {
                get { return new BloggingContextWithMigrationsModelSnapshot().Model; }
            }

            public override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable("Blog",
                c => new
                {
                    BlogId = c.Column("int", annotations: new Dictionary<string, string> { { "SqlServer:ValueGeneration", "Identity" } }),
                    Name = c.Column("nvarchar(max)", nullable: true),
                })
                .PrimaryKey(t => t.BlogId, name: "PK_Blog");
            }

            public override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable("Blog");
            }
        }

        [ContextType(typeof(BloggingContextWithMigrations))]
        public class MigrationTwo : Migration
        {
            public override string Id
            {
                get { return "222222222222222_MigrationTwo"; }
            }

            public override string ProductVersion
            {
                get { return CurrentProductVersion; }
            }

            public override IModel Target
            {
                get { return new BloggingContextWithMigrationsModelSnapshot().Model; }
            }

            public override void Up(MigrationBuilder migrationBuilder)
            { }

            public override void Down(MigrationBuilder migrationBuilder)
            { }
        }
    }
}