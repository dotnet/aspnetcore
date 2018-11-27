// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core.Builder
{
    public class MvcApplicationBuilderExtensionsTest
    {
        [Fact]
        public void UseMvc_ThrowsInvalidOperationException_IfMvcMarkerServiceIsNotRegistered()
        {
            // Arrange
            var applicationBuilderMock = new Mock<IApplicationBuilder>();
            applicationBuilderMock
                .Setup(s => s.ApplicationServices)
                .Returns(Mock.Of<IServiceProvider>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => applicationBuilderMock.Object.UseMvc(rb => { }));

            Assert.Equal(
                "Unable to find the required services. Please add all the required services by calling " +
                "'IServiceCollection.AddMvc' inside the call to 'ConfigureServices(...)' " +
                "in the application startup code.",
                exception.Message);
        }

        [Fact]
        public void UseMvc_EndpointRoutingDisabled_NoEndpointInfos()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<DiagnosticListener>(new DiagnosticListener("Microsoft.AspNetCore"));
            services.AddLogging();
            services.AddMvcCore(o => o.EnableEndpointRouting = false);
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            appBuilder.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            var routeOptions = appBuilder.ApplicationServices
                .GetRequiredService<IOptions<RouteOptions>>();

            Assert.Empty(routeOptions.Value.EndpointDataSources);
        }

        [Fact]
        public void UseMvc_EndpointRoutingEnabled_NoEndpointInfos()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<DiagnosticListener>(new DiagnosticListener("Microsoft.AspNetCore"));
            services.AddLogging();
            services.AddMvcCore(o => o.EnableEndpointRouting = true);
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            appBuilder.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            var routeOptions = appBuilder.ApplicationServices
                .GetRequiredService<IOptions<RouteOptions>>();

            var mvcEndpointDataSource = (MvcEndpointDataSource)Assert.Single(routeOptions.Value.EndpointDataSources, ds => ds is MvcEndpointDataSource);

            var endpointInfo = Assert.Single(mvcEndpointDataSource.ConventionalEndpointInfos);
            Assert.Equal("default", endpointInfo.Name);
            Assert.Equal("{controller=Home}/{action=Index}/{id?}", endpointInfo.Pattern);
        }
    }
}
