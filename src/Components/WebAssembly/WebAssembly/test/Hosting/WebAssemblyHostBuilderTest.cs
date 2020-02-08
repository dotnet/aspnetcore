// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    public class WebAssemblyHostBuilderTest
    {
        [Fact]
        public void Build_AllowsConfiguringConfiguration()
        {
            // Arrange
            var builder = WebAssemblyHostBuilder.CreateDefault();

            builder.Configuration.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("key", "value"),
            });

            // Act
            var host = builder.Build();

            // Assert
            Assert.Equal("value", host.Configuration["key"]);
        }

        [Fact]
        public void Build_AllowsConfiguringServices()
        {
            // Arrange
            var builder = WebAssemblyHostBuilder.CreateDefault();

            // This test also verifies that we create a scope.
            builder.Services.AddScoped<StringBuilder>();

            // Act
            var host = builder.Build();

            // Assert
            Assert.NotNull(host.Services.GetRequiredService<StringBuilder>());
        }

        [Fact]
        public void Build_AllowsConfiguringContainer()
        {
            // Arrange
            var builder = WebAssemblyHostBuilder.CreateDefault();

            builder.Services.AddScoped<StringBuilder>();
            var factory = new MyFakeServiceProviderFactory();
            builder.ConfigureContainer(factory);

            // Act
            var host = builder.Build();

            // Assert
            Assert.True(factory.CreateServiceProviderCalled);
            Assert.NotNull(host.Services.GetRequiredService<StringBuilder>());
        }

        [Fact]
        public void Build_AllowsConfiguringContainer_WithDelegate()
        {
            // Arrange
            var builder = WebAssemblyHostBuilder.CreateDefault();

            builder.Services.AddScoped<StringBuilder>();

            var factory = new MyFakeServiceProviderFactory();
            builder.ConfigureContainer(factory, builder =>
            {
                builder.ServiceCollection.AddScoped<List<string>>();
            });

            // Act
            var host = builder.Build();

            // Assert
            Assert.True(factory.CreateServiceProviderCalled);
            Assert.NotNull(host.Services.GetRequiredService<StringBuilder>());
            Assert.NotNull(host.Services.GetRequiredService<List<string>>());
        }

        private class MyFakeDIBuilderThing
        {
            public MyFakeDIBuilderThing(IServiceCollection serviceCollection)
            {
                ServiceCollection = serviceCollection;
            }

            public IServiceCollection ServiceCollection { get; }
        }

        private class MyFakeServiceProviderFactory : IServiceProviderFactory<MyFakeDIBuilderThing>
        {
            public bool CreateServiceProviderCalled { get; set; }

            public MyFakeDIBuilderThing CreateBuilder(IServiceCollection services)
            {
                return new MyFakeDIBuilderThing(services);
            }

            public IServiceProvider CreateServiceProvider(MyFakeDIBuilderThing containerBuilder)
            {
                // This is the best way to test the factory was actually used. The Host doesn't
                // expose the *root* service provider, only a scoped instance. So we can return
                // a different type here, but we have no way to inspect it.
                CreateServiceProviderCalled = true;
                return containerBuilder.ServiceCollection.BuildServiceProvider();
            }
        }

        [Fact]
        public void Build_AddsConfigurationToServices()
        {
            // Arrange
            var builder = WebAssemblyHostBuilder.CreateDefault();

            builder.Configuration.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("key", "value"),
            });

            // Act
            var host = builder.Build();

            // Assert
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            Assert.Equal("value", configuration["key"]);
        }

        private static IReadOnlyList<Type> DefaultServiceTypes
        {
            get
            {
                return new Type[]
                {
                    typeof(IJSRuntime),
                    typeof(NavigationManager),
                    typeof(INavigationInterception),
                    typeof(ILoggerFactory),
                    typeof(ILogger<>),
                };
            }
        }

        [Fact]
        public void Constructor_AddsDefaultServices()
        {
            // Arrange & Act
            var builder = WebAssemblyHostBuilder.CreateDefault();

            // Assert
            Assert.Equal(DefaultServiceTypes.Count, builder.Services.Count);
            foreach (var type in DefaultServiceTypes)
            {
                Assert.Single(builder.Services, d => d.ServiceType == type);
            }
        }
    }
}
