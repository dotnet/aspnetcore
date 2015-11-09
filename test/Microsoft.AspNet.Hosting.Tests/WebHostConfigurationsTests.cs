// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Builder.Internal;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class WebHostConfigurationTests
    {
        [Fact]
        public void ReadsParametersCorrectly()
        {
            var parameters = new Dictionary<string, string>()
            {
                {"webroot", "wwwroot"},
                {"server", "Microsoft.AspNet.Server.Kestrel"},
                {"app", "MyProjectReference"},
                {"environment", "Development"},
                {"detailederrors", "true"},
            };

            var config = new WebHostOptions(new ConfigurationBuilder().AddInMemoryCollection(parameters).Build());

            Assert.Equal("wwwroot", config.WebRoot);
            Assert.Equal("Microsoft.AspNet.Server.Kestrel", config.Server);
            Assert.Equal("MyProjectReference", config.Application);
            Assert.Equal("Development", config.Environment);
            Assert.Equal(true, config.DetailedErrors);
        }

        [Fact]
        public void ReadsOldEnvKey()
        {
            var parameters = new Dictionary<string, string>() { { "ENV", "Development" } };
            var config = new WebHostOptions(new ConfigurationBuilder().AddInMemoryCollection(parameters).Build());

            Assert.Equal("Development", config.Environment);
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
