// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Hosting
{
    // Most functionality is covered by WebHostBuilderTests for compat. Only GenericHost specific functionality is covered here.
    public class GenericWebHostBuilderTests
    {
        [Fact]
        public void ReadsAspNetCoreEnvironmentVariables()
        {
            var randomEnvKey = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("ASPNETCORE_" + randomEnvKey, "true");
            using var host = new HostBuilder()
                .ConfigureWebHost(_ => { })
                .Build();
            var config = host.Services.GetRequiredService<IConfiguration>();
            Assert.Equal("true", config[randomEnvKey]);
            Environment.SetEnvironmentVariable("ASPNETCORE_" + randomEnvKey, null);
        }

        [Fact]
        public void CanSuppressAspNetCoreEnvironmentVariables()
        {
            var randomEnvKey = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("ASPNETCORE_" + randomEnvKey, "true");
            using var host = new HostBuilder()
                .ConfigureWebHost(_ => { }, webHostBulderOptions => { webHostBulderOptions.SuppressEnvironmentConfiguration  = true; })
                .Build();
            var config = host.Services.GetRequiredService<IConfiguration>();
            Assert.Null(config[randomEnvKey]);
            Environment.SetEnvironmentVariable("ASPNETCORE_" + randomEnvKey, null);
        }
    }
}
