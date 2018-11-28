// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.Hosting
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
        public void HostBuilder_ServiceFactory_Autofac()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();
            builder.ConfigureServices((c, s) => s.AddSingleton<IServiceProviderFactory<IServiceCollection>>(new MyServiceProviderFactory()));

            // Act
            var host = builder.Build();

            // Assert
            Assert.IsType<AutofacServiceProvider>(host.Services);
        }

        private class MyServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
        {
            public IServiceCollection CreateBuilder(IServiceCollection services)
            {
                return new MyServiceCollection(services);
            }

            public IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
            {
                Assert.IsType<MyServiceCollection>(serviceCollection);
                var containerBuilder = new ContainerBuilder();
                containerBuilder.Populate(serviceCollection);
                var container = containerBuilder.Build();
                return new AutofacServiceProvider(container);
            }
        }

        private class MyServiceCollection : List<ServiceDescriptor>, IServiceCollection
        {
            public MyServiceCollection(IEnumerable<ServiceDescriptor> collection) : base(collection)
            {
            }
        }

    }
}
