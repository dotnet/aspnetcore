// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class WebHostConfigurationTests
    {
        [Fact]
        public void ReadsParametersCorrectly()
        {
            var parameters = new Dictionary<string, string>()
            {
                { WebHostDefaults.WebRootKey, "wwwroot"},
                { WebHostDefaults.ApplicationKey, "MyProjectReference"},
                { WebHostDefaults.StartupAssemblyKey, "MyProjectReference" },
                { WebHostDefaults.EnvironmentKey, Environments.Development},
                { WebHostDefaults.DetailedErrorsKey, "true"},
                { WebHostDefaults.CaptureStartupErrorsKey, "true" },
                { WebHostDefaults.SuppressStatusMessagesKey, "true" }
            };

            var config = new WebHostOptions(new ConfigurationBuilder().AddInMemoryCollection(parameters).Build());

            Assert.Equal("wwwroot", config.WebRoot);
            Assert.Equal("MyProjectReference", config.ApplicationName);
            Assert.Equal("MyProjectReference", config.StartupAssembly);
            Assert.Equal(Environments.Development, config.Environment);
            Assert.True(config.CaptureStartupErrors);
            Assert.True(config.DetailedErrors);
            Assert.True(config.SuppressStatusMessages);
        }

        [Fact]
        public void ReadsOldEnvKey()
        {
            var parameters = new Dictionary<string, string>() { { "ENVIRONMENT", Environments.Development } };
            var config = new WebHostOptions(new ConfigurationBuilder().AddInMemoryCollection(parameters).Build());

            Assert.Equal(Environments.Development, config.Environment);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void AllowsNumberForDetailedErrors(string value, bool expected)
        {
            var parameters = new Dictionary<string, string>() { { "detailedErrors", value } };
            var config = new WebHostOptions(new ConfigurationBuilder().AddInMemoryCollection(parameters).Build());

            Assert.Equal(expected, config.DetailedErrors);
        }
    }
}
