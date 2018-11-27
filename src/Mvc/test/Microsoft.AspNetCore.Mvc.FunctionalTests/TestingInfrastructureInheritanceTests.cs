// Copyright (c) .NET  Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TestingInfrastructureInheritanceTests
    {
        [Fact]
        public void TestingInfrastructure_WithWebHostBuilderRespectsCustomizations()
        {
            // Act
            var factory = new CustomizedFactory<BasicWebSite.Startup>();
            var customized = factory
                .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("Customization"))
                .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("FurtherCustomization"));
            var client = customized.CreateClient();

            // Assert
            Assert.Equal(new[] { "ConfigureWebHost", "Customization", "FurtherCustomization" }, factory.ConfigureWebHostCalled.ToArray());
            Assert.True(factory.CreateServerCalled);
            Assert.True(factory.CreateWebHostBuilderCalled);
            Assert.True(factory.GetTestAssembliesCalled);
        }

        private class CustomizedFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
        {
            public bool GetTestAssembliesCalled { get; private set; }
            public bool CreateWebHostBuilderCalled { get; private set; }
            public bool CreateServerCalled { get; private set; }
            public IList<string> ConfigureWebHostCalled { get; private set; } = new List<string>();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                ConfigureWebHostCalled.Add("ConfigureWebHost");
                base.ConfigureWebHost(builder);
            }

            protected override TestServer CreateServer(IWebHostBuilder builder)
            {
                CreateServerCalled = true;
                return base.CreateServer(builder);
            }

            protected override IWebHostBuilder CreateWebHostBuilder()
            {
                CreateWebHostBuilderCalled = true;
                return base.CreateWebHostBuilder();
            }

            protected override IEnumerable<Assembly> GetTestAssemblies()
            {
                GetTestAssembliesCalled = true;
                return base.GetTestAssemblies();
            }
        }
    }
}
