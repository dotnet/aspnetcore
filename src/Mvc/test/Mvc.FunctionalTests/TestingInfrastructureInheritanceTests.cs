// Copyright (c) .NET  Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TestingInfrastructureInheritanceTests
    {
        [Fact]
        public void TestingInfrastructure_WebHost_WithWebHostBuilderRespectsCustomizations()
        {
            // Act
            using var factory = new CustomizedFactory<BasicWebSite.StartupWithoutEndpointRouting>();
            using var customized = factory
                .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("Customization"))
                .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("FurtherCustomization"));
            var client = customized.CreateClient();

            // Assert
            Assert.Equal(new[] { "ConfigureWebHost", "Customization", "FurtherCustomization" }, factory.ConfigureWebHostCalled.ToArray());
            Assert.True(factory.CreateServerCalled);
            Assert.True(factory.CreateWebHostBuilderCalled);
            Assert.True(factory.GetTestAssembliesCalled);
            Assert.True(factory.CreateHostBuilderCalled);
            Assert.False(factory.CreateHostCalled);
        }

        [Fact]
        public void TestingInfrastructure_GenericHost_WithWithHostBuilderRespectsCustomizations()
        {
            // Act
            using var factory = new CustomizedFactory<GenericHostWebSite.Startup>();
            using var customized = factory
                .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("Customization"))
                .WithWebHostBuilder(builder => factory.ConfigureWebHostCalled.Add("FurtherCustomization"));
            var client = customized.CreateClient();

            // Assert
            Assert.Equal(new[] { "ConfigureWebHost", "Customization", "FurtherCustomization" }, factory.ConfigureWebHostCalled.ToArray());
            Assert.True(factory.GetTestAssembliesCalled);
            Assert.True(factory.CreateHostBuilderCalled);
            Assert.True(factory.CreateHostCalled);
            Assert.False(factory.CreateServerCalled);
            Assert.False(factory.CreateWebHostBuilderCalled);
        }

        [Fact]
        public void TestingInfrastructure_GenericHost_WithWithHostBuilderHasServices()
        {
            // Act
            using var factory = new CustomizedFactory<GenericHostWebSite.Startup>();

            // Assert
            Assert.NotNull(factory.Services);
            Assert.NotNull(factory.Services.GetService(typeof(IConfiguration)));
        }

        private class CustomizedFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
        {
            public bool GetTestAssembliesCalled { get; private set; }
            public bool CreateWebHostBuilderCalled { get; private set; }
            public bool CreateHostBuilderCalled { get; private set; }
            public bool CreateServerCalled { get; private set; }
            public bool CreateHostCalled { get; private set; }
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

            protected override IHost CreateHost(IHostBuilder builder)
            {
                CreateHostCalled = true;
                return base.CreateHost(builder);
            }

            protected override IWebHostBuilder CreateWebHostBuilder()
            {
                CreateWebHostBuilderCalled = true;
                return base.CreateWebHostBuilder();
            }

            protected override IHostBuilder CreateHostBuilder()
            {
                CreateHostBuilderCalled = true;
                return base.CreateHostBuilder();
            }

            protected override IEnumerable<Assembly> GetTestAssemblies()
            {
                GetTestAssembliesCalled = true;
                return base.GetTestAssemblies();
            }
        }
    }
}
