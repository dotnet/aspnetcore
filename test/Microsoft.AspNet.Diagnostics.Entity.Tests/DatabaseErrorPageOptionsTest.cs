// Copyright (c) .NET Foundation. All rights reserved.
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
        }

        [Fact]
        public void ShowAll_shows_all_errors()
        {
            var options = DatabaseErrorPageOptions.ShowAll;

            Assert.True(options.ShowExceptionDetails);
            Assert.True(options.ListMigrations);
        }

        [Fact]
        public void ShowExceptionDetails_is_respected()
        {
            var options = DatabaseErrorPageOptions.ShowAll;
            options.ShowExceptionDetails = false;

            Assert.False(options.ShowExceptionDetails);
        }

        [Fact]
        public void ListMigrations_is_respected()
        {
            var options = DatabaseErrorPageOptions.ShowAll;
            options.ListMigrations = false;

            Assert.False(options.ListMigrations);
        }
    }
}