// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Migrations.Builders;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using System;
using System.Linq;
using System.Reflection;

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
        public class MigrationOne : Migration, IMigrationMetadata
        {
            string IMigrationMetadata.MigrationId
            {
                get { return "111111111111111_MigrationOne"; }
            }

            string IMigrationMetadata.ProductVersion
            {
                get { return CurrentProductVersion; }
            }

            IModel IMigrationMetadata.TargetModel
            {
                get { return new BloggingContextWithMigrationsModelSnapshot().Model; }
            }

            public override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable("Blog",
                c => new
                {
                    BlogId = c.Int(nullable: false, identity: true),
                    Name = c.String(),
                })
                .PrimaryKey("PK_Blog", t => t.BlogId);
            }

            public override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable("Blog");
            }
        }

        [ContextType(typeof(BloggingContextWithMigrations))]
        public class MigrationTwo : Migration, IMigrationMetadata
        {
            string IMigrationMetadata.MigrationId
            {
                get { return "222222222222222_MigrationTwo"; }
            }

            string IMigrationMetadata.ProductVersion
            {
                get { return CurrentProductVersion; }
            }

            IModel IMigrationMetadata.TargetModel
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