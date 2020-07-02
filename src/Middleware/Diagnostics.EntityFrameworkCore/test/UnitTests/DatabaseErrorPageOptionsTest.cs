// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests
{
    public class DatabaseErrorPageOptionsTest
    {
        [Fact]
        public void Empty_MigrationsEndPointPath_by_default()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var options = new DatabaseErrorPageOptions();
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal(new PathString("/ApplyDatabaseMigrations"), options.MigrationsEndPointPath);
        }

        [Fact]
        public void MigrationsEndPointPath_is_respected()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var options = new DatabaseErrorPageOptions();
#pragma warning restore CS0618 // Type or member is obsolete
            options.MigrationsEndPointPath = "/test";
            
            Assert.Equal("/test", options.MigrationsEndPointPath);
        }
    }
}
