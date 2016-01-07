// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class DatabaseErrorPageOptionsTest
    {
        [Fact]
        public void Empty_MigrationsEndPointPath_by_default()
        {
            var options = new DatabaseErrorPageOptions();

            Assert.Equal(MigrationsEndPointOptions.DefaultPath, options.MigrationsEndPointPath);
        }

        [Fact]
        public void MigrationsEndPointPath_is_respected()
        {
            var options = new DatabaseErrorPageOptions();
            options.MigrationsEndPointPath = "/test";
            
            Assert.Equal("/test", options.MigrationsEndPointPath);
        }
    }
}