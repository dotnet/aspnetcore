// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class DatabaseErrorPageOptionsTest
    {
        [Fact]
        public void Default_visibility_is_false()
        {
            var options = new DatabaseErrorPageOptions();

            Assert.False(options.ShowExceptionDetails);
            Assert.False(options.ListMigrations);
            Assert.False(options.EnableMigrationCommands);
        }

        [Fact]
        public void Default_visibility_can_be_changed()
        {
            var options = new DatabaseErrorPageOptions();
            options.SetDefaultVisibility(true);

            Assert.True(options.ShowExceptionDetails);
            Assert.True(options.ListMigrations);
            Assert.True(options.EnableMigrationCommands);
        }

        [Fact]
        public void ShowExceptionDetails_overides_default_visibility()
        {
            var options = new DatabaseErrorPageOptions { ShowExceptionDetails = false };
            options.SetDefaultVisibility(true);

            Assert.False(options.ShowExceptionDetails);
            Assert.True(options.ListMigrations);
            Assert.True(options.EnableMigrationCommands);
        }

        [Fact]
        public void ListMigrations_overides_default_visibility()
        {
            var options = new DatabaseErrorPageOptions { ListMigrations = false };
            options.SetDefaultVisibility(true);

            Assert.True(options.ShowExceptionDetails);
            Assert.False(options.ListMigrations);
            Assert.True(options.EnableMigrationCommands);
        }

        [Fact]
        public void EnableMigrationCommands_overides_default_visibility()
        {
            var options = new DatabaseErrorPageOptions { EnableMigrationCommands = false };
            options.SetDefaultVisibility(true);

            Assert.True(options.ShowExceptionDetails);
            Assert.True(options.ListMigrations);
            Assert.False(options.EnableMigrationCommands);
        }
    }
}