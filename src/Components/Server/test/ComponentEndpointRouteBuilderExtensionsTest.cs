// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
            var applicationBuilder = new ApplicationBuilder(
                new ServiceCollection()
                .AddLogging()
                .AddSingleton(Mock.Of<IHostApplicationLifetime>())
                .AddSignalR().Services
                .AddServerSideBlazor().Services.BuildServiceProvider());
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
            var applicationBuilder = new ApplicationBuilder(
                new ServiceCollection()
                .AddLogging()
                .AddSingleton(Mock.Of<IHostApplicationLifetime>())
                .AddSignalR().Services
                .AddServerSideBlazor().Services.BuildServiceProvider());
            var called = false;

            // Act
            var app = applicationBuilder
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapBlazorHub(Mock.Of<IComponent>().GetType(),"app", "_blazor", dispatchOptions => called = true);
                }).Build();

            // Assert
            Assert.True(called);
        }
    }
}
