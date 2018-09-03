// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Builder
{
    public class EndpointRoutingApplicationBuilderExtensionsTest
    {
        [Fact]
        public void UseEndpointRouting_ServicesNotRegistered_Throws()
        {
            // Arrange
            var app = new ApplicationBuilder(Mock.Of<IServiceProvider>());

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => app.UseEndpointRouting());

            // Assert
            Assert.Equal(
                "Unable to find the required services. " +
                "Please add all the required services by calling 'IServiceCollection.AddRouting' " +
                "inside the call to 'ConfigureServices(...)' in the application startup code.",
                ex.Message);
        }

        [Fact]
        public void UseEndpoint_ServicesNotRegistered_Throws()
        {
            // Arrange
            var app = new ApplicationBuilder(Mock.Of<IServiceProvider>());

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => app.UseEndpoint());

            // Assert
            Assert.Equal(
                "Unable to find the required services. " +
                "Please add all the required services by calling 'IServiceCollection.AddRouting' " +
                "inside the call to 'ConfigureServices(...)' in the application startup code.",
                ex.Message);
        }

        [Fact]
        public async Task UseEndpointRouting_ServicesRegistered_NoMatch_DoesNotSetFeature()
        {
            // Arrange
            var services = CreateServices();

            var app = new ApplicationBuilder(services);

            app.UseEndpointRouting();

            var appFunc = app.Build();
            var httpContext = new DefaultHttpContext();

            // Act
            await appFunc(httpContext);

            // Assert
            Assert.Null(httpContext.Features.Get<IEndpointFeature>());
        }

        [Fact]
        public async Task UseEndpointRouting_ServicesRegistered_Match_DoesNotSetsFeature()
        {
            // Arrange
            var endpoint = new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse("{*p}"),
                0,
                EndpointMetadataCollection.Empty,
                "Test");

            var services = CreateServices(endpoint);

            var app = new ApplicationBuilder(services);

            app.UseEndpointRouting();

            var appFunc = app.Build();
            var httpContext = new DefaultHttpContext();

            // Act
            await appFunc(httpContext);

            // Assert
            var feature = httpContext.Features.Get<IEndpointFeature>();
            Assert.NotNull(feature);
            Assert.Same(endpoint, feature.Endpoint);
        }

        [Fact]
        public void UseEndpoint_WithoutRoutingServicesRegistered_Throws()
        {
            // Arrange
            var services = CreateServices();

            var app = new ApplicationBuilder(services);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => app.UseEndpoint());

            // Assert
            Assert.Equal(
                "EndpointRoutingMiddleware must be added to the request execution pipeline before EndpointMiddleware. " +
                "Please add EndpointRoutingMiddleware by calling 'IApplicationBuilder.UseEndpointRouting' " +
                "inside the call to 'Configure(...)' in the application startup code.",
                ex.Message);
        }

        [Fact]
        public async Task UseEndpoint_ServicesRegisteredAndEndpointRoutingRegistered_NoMatch_DoesNotSetFeature()
        {
            // Arrange
            var services = CreateServices();

            var app = new ApplicationBuilder(services);

            app.UseEndpointRouting();
            app.UseEndpoint();

            var appFunc = app.Build();
            var httpContext = new DefaultHttpContext();

            // Act
            await appFunc(httpContext);

            // Assert
            Assert.Null(httpContext.Features.Get<IEndpointFeature>());
        }

        [Fact]
        public void UseEndpointRouting_CallWithBuilder_SetsEndpointBuilder()
        {
            // Arrange
            var services = CreateServices();

            var app = new ApplicationBuilder(services);

            // Act
            app.UseEndpointRouting(builder =>
            {
                builder.MapEndpoint(d => null, "/", "Test endpoint");
            });

            // Assert
            var dataSourceBuilder = (DefaultEndpointDataSourceBuilder)services.GetRequiredService<EndpointDataSourceBuilder>();
            var endpointBuilder = Assert.Single(dataSourceBuilder.Endpoints);
            Assert.Equal("Test endpoint", endpointBuilder.DisplayName);
        }

        private IServiceProvider CreateServices(params Endpoint[] endpoints)
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddOptions();
            services.AddRouting();

            services.AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource(endpoints));

            return services.BuildServiceProvider();
        }
    }
}
