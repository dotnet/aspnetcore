// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Hosting.Test
{
    public class WebAssemblyHostBuilderTest
    {
        [Fact]
        public void HostBuilder_CanCallBuild_BuildsServices()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();

            // Act
            var host = builder.Build();

            // Assert
            Assert.NotNull(host.Services.GetService(typeof(IWebAssemblyHost)));
        }

        [Fact]
        public void HostBuilder_CanConfigureAdditionalServices()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();
            builder.ConfigureServices((c, s) => s.AddSingleton<string>("foo"));
            builder.ConfigureServices((c, s) => s.AddSingleton<StringBuilder>(new StringBuilder("bar")));

            // Act
            var host = builder.Build();

            // Assert
            Assert.Equal("foo", host.Services.GetService(typeof(string)));
            Assert.Equal("bar", host.Services.GetService(typeof(StringBuilder)).ToString());
        }

        [Fact]
        public void HostBuilder_UseBlazorStartup_CanConfigureAdditionalServices()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();
            builder.UseBlazorStartup<MyStartup>();
            builder.ConfigureServices((c, s) => s.AddSingleton<StringBuilder>(new StringBuilder("bar")));

            // Act
            var host = builder.Build();

            // Assert
            Assert.Equal("foo", host.Services.GetService(typeof(string)));
            Assert.Equal("bar", host.Services.GetService(typeof(StringBuilder)).ToString());
        }

        [Fact]
        public void HostBuilder_UseBlazorStartup_DoesNotAllowMultiple()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();
            builder.UseBlazorStartup<MyStartup>();

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => builder.UseBlazorStartup<MyStartup>());

            // Assert
            Assert.Equal("A startup class has already been registered.", ex.Message);
        }

        private class MyStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton<string>("foo");
            }
        }

        [Fact]
        public void HostBuilder_CanCustomizeServiceFactory()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();
            builder.UseServiceProviderFactory(new TestServiceProviderFactory());

            // Act
            var host = builder.Build();

            // Assert
            Assert.IsType<TestServiceProvider>(host.Services);
        }

        [Fact]
        public void HostBuilder_CanCustomizeServiceFactoryWithContext()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();
            builder.UseServiceProviderFactory(context =>
            {
                Assert.NotNull(context.Properties);
                Assert.Same(builder.Properties, context.Properties);
                return new TestServiceProviderFactory();
            });

            // Act
            var host = builder.Build();

            // Assert
            Assert.IsType<TestServiceProvider>(host.Services);
        }

        private class TestServiceProvider : IServiceProvider
        {
            private readonly IServiceProvider _underlyingProvider;

            public TestServiceProvider(IServiceProvider underlyingProvider)
            {
                _underlyingProvider = underlyingProvider;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IWebAssemblyHost))
                {
                    // Since the test will make assertions about the resulting IWebAssemblyHost,
                    // show that custom DI containers have the power to substitute themselves
                    // as the IServiceProvider
                    return new WebAssemblyHost(
                        this, _underlyingProvider.GetRequiredService<IJSRuntime>());
                }
                else
                {
                    return _underlyingProvider.GetService(serviceType);
                }
            }
        }

        private class TestServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
        {
            public IServiceCollection CreateBuilder(IServiceCollection services)
            {
                return new TestServiceCollection(services);
            }

            public IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
            {
                Assert.IsType<TestServiceCollection>(serviceCollection);
                return new TestServiceProvider(serviceCollection.BuildServiceProvider());
            }

            class TestServiceCollection : List<ServiceDescriptor>, IServiceCollection
            {
                public TestServiceCollection(IEnumerable<ServiceDescriptor> collection)
                    : base(collection)
                {
                }
            }
        }
    }
}
