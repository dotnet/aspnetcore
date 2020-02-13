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
                    typeof(HttpClient),
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
