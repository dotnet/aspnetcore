// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Tests
{
    public class ComponentEndpointRouteBuilderExtensionsTest
    {
        [Fact]
        public void MapBlazorHub_WiresUp_UnderlyingHub()
        {
            // Arrange
            var applicationBuilder = CreateAppBuilder();
            var called = false;

            // Act
            var app = applicationBuilder
                .UseRouting()
                .UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub(dispatchOptions => called = true);
            }).Build();

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void MapBlazorHub_MostGeneralOverload_MapsUnderlyingHub()
        {
            // Arrange
            var applicationBuilder = CreateAppBuilder();
            var called = false;

            // Act
            var app = applicationBuilder
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapBlazorHub("_blazor", dispatchOptions => called = true);
                }).Build();

            // Assert
            Assert.True(called);
        }
        
        private IApplicationBuilder CreateAppBuilder()
        {
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
            services.AddLogging();
            services.AddOptions();
            var listener = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton(listener);
            services.AddSingleton<DiagnosticSource>(listener);
            services.AddRouting();
            services.AddSignalR();
            services.AddServerSideBlazor();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            var serviceProvider = services.BuildServiceProvider();

            return new ApplicationBuilder(serviceProvider);
        }

        private class MyComponent : IComponent
        {
            public void Attach(RenderHandle renderHandle)
            {
                throw new System.NotImplementedException();
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
