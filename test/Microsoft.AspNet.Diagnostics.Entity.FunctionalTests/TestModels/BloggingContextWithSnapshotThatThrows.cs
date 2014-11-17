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
    public class BloggingContextWithSnapshotThatThrows : BloggingContext
    {
        public BloggingContextWithSnapshotThatThrows(IServiceProvider provider, DbContextOptions options)
            : base(provider, options)
        { }

        [ContextType(typeof(BloggingContextWithSnapshotThatThrows))]
        public class BloggingContextWithSnapshotThatThrowsModelSnapshot : ModelSnapshot
        {
            public override IModel Model
            {
                get
                {
                    throw new Exception("Welcome to the invalid snapshot!");
                }
            }
        }

        [ContextType(typeof(BloggingContextWithSnapshotThatThrows))]
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
                get { return new BloggingContextWithSnapshotThatThrowsModelSnapshot().Model; }
            }

            public override void Up(MigrationBuilder migrationBuilder)
            {
                throw new Exception("Welcome to the invalid migration!");
            }

            public override void Down(MigrationBuilder migrationBuilder)
            { }
        }
    }
}