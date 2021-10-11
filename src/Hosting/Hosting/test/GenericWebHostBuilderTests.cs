// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
