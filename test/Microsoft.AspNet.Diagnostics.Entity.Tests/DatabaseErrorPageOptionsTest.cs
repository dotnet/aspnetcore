// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class DatabaseErrorPageOptionsTest
    {
        [Fact]
        public void Everything_disabled_by_default()
        {
            var options = new DatabaseErrorPageOptions();

            Assert.False(options.ShowExceptionDetails);
            Assert.False(options.ListMigrations);
            Assert.False(options.EnableMigrationCommands);
            Assert.Equal(string.Empty, options.MigrationsEndPointPath);
        }

        [Fact]
        public void EnableAll_enables_everything()
        {
            var options = new DatabaseErrorPageOptions();
            options.EnableAll();

            Assert.True(options.ShowExceptionDetails);
            Assert.True(options.ListMigrations);
            Assert.True(options.EnableMigrationCommands);
            Assert.Equal(MigrationsEndPointOptions.DefaultPath, options.MigrationsEndPointPath);
        }

        [Fact]
        public void ShowExceptionDetails_is_respected()
        {
            var options = new DatabaseErrorPageOptions();
            options.EnableAll();
            options.ShowExceptionDetails = false;

            Assert.False(options.ShowExceptionDetails);
            Assert.True(options.ListMigrations);
            Assert.True(options.EnableMigrationCommands);
            Assert.Equal(MigrationsEndPointOptions.DefaultPath, options.MigrationsEndPointPath);
        }

        [Fact]
        public void ListMigrations_is_respected()
        {
            var options = new DatabaseErrorPageOptions();
            options.EnableAll();
            options.ListMigrations = false;

            Assert.True(options.ShowExceptionDetails);
            Assert.False(options.ListMigrations);
            Assert.True(options.EnableMigrationCommands);
            Assert.Equal(MigrationsEndPointOptions.DefaultPath, options.MigrationsEndPointPath);
        }

        [Fact]
        public void EnableMigrationCommands_is_respected()
        {
            var options = new DatabaseErrorPageOptions();
            options.EnableAll();
            options.EnableMigrationCommands = false;

            Assert.True(options.ShowExceptionDetails);
            Assert.True(options.ListMigrations);
            Assert.False(options.EnableMigrationCommands);
            Assert.Equal(MigrationsEndPointOptions.DefaultPath, options.MigrationsEndPointPath);
        }

        [Fact]
        public void MigrationsEndPointPath_is_respected()
        {
            var options = new DatabaseErrorPageOptions();
            options.EnableAll();
            options.MigrationsEndPointPath = "/test";

            Assert.True(options.ShowExceptionDetails);
            Assert.True(options.ListMigrations);
            Assert.True(options.EnableMigrationCommands);
            Assert.Equal("/test", options.MigrationsEndPointPath);
        }
    }
}