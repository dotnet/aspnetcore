// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class WebApplicationConfigurationTests
    {
        [Fact]
        public void ReadsParametersCorrectly()
        {
            var parameters = new Dictionary<string, string>()
            {
                { "webroot", "wwwroot"},
                { "server", "Microsoft.AspNet.Server.Kestrel"},
                { "application", "MyProjectReference"},
                { "environment", "Development"},
                { "detailederrors", "true"},
                { "captureStartupErrors", "true" }
            };

            var config = new WebApplicationOptions(new ConfigurationBuilder().AddInMemoryCollection(parameters).Build());

            Assert.Equal("wwwroot", config.WebRoot);
            Assert.Equal("Microsoft.AspNet.Server.Kestrel", config.ServerFactoryLocation);
            Assert.Equal("MyProjectReference", config.Application);
            Assert.Equal("Development", config.Environment);
            Assert.True(config.CaptureStartupErrors);
            Assert.True(config.DetailedErrors);
        }

        [Fact]
        public void ReadsOldEnvKey()
        {
            var parameters = new Dictionary<string, string>() { { "ENV", "Development" } };
            var config = new WebApplicationOptions(new ConfigurationBuilder().AddInMemoryCollection(parameters).Build());

            Assert.Equal("Development", config.Environment);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void AllowsNumberForDetailedErrors(string value, bool expected)
        {
            var parameters = new Dictionary<string, string>() { { "detailedErrors", value } };
            var config = new WebApplicationOptions(new ConfigurationBuilder().AddInMemoryCollection(parameters).Build());

            Assert.Equal(expected, config.DetailedErrors);
        }
    }
}
