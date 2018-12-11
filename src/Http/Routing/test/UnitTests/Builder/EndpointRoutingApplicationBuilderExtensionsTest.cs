// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Builder
{
    public class EndpointRoutingApplicationBuilderExtensionsTest
    {
        [Fact]
        public void UseRouting_ServicesNotRegistered_Throws()
        {
            // Arrange
            var app = new ApplicationBuilder(Mock.Of<IServiceProvider>());

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => app.UseRouting(builder => { }));

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
        public async Task UseRouting_ServicesRegistered_NoMatch_DoesNotSetFeature()
        {
            // Arrange
            var services = CreateServices();

            var app = new ApplicationBuilder(services);

            app.UseRouting(builder => { });

            var appFunc = app.Build();
            var httpContext = new DefaultHttpContext();

            // Act
            await appFunc(httpContext);

            // Assert
            Assert.Null(httpContext.Features.Get<IEndpointFeature>());
        }

        [Fact]
        public async Task UseRouting_ServicesRegistered_Match_DoesNotSetsFeature()
        {
            // Arrange
            var endpoint = new RouteEndpoint(
               TestConstants.EmptyRequestDelegate,
               RoutePatternFactory.Parse("{*p}"),
               0,
               EndpointMetadataCollection.Empty,
               "Test");

            var services = CreateServices();

            var app = new ApplicationBuilder(services);

            app.UseRouting(builder =>
            {
                builder.DataSources.Add(new DefaultEndpointDataSource(endpoint));
            });

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
                "Please add EndpointRoutingMiddleware by calling 'IApplicationBuilder.UseRouting' " +
                "inside the call to 'Configure(...)' in the application startup code.",
                ex.Message);
        }

        [Fact]
        public async Task UseEndpoint_ServicesRegisteredAndEndpointRoutingRegistered_NoMatch_DoesNotSetFeature()
        {
            // Arrange
            var services = CreateServices();

            var app = new ApplicationBuilder(services);

            app.UseRouting(builder => { });
            app.UseEndpoint();

            var appFunc = app.Build();
            var httpContext = new DefaultHttpContext();

            // Act
            await appFunc(httpContext);

            // Assert
            Assert.Null(httpContext.Features.Get<IEndpointFeature>());
        }

        [Fact]
        public void UseRouting_CallWithBuilder_SetsEndpointDataSource()
        {
            // Arrange
            var matcherEndpointDataSources = new List<EndpointDataSource>();
            var matcherFactoryMock = new Mock<MatcherFactory>();
            matcherFactoryMock
                .Setup(m => m.CreateMatcher(It.IsAny<EndpointDataSource>()))
                .Callback((EndpointDataSource arg) =>
                {
                    matcherEndpointDataSources.Add(arg);
                })
                .Returns(new TestMatcher(false));

            var services = CreateServices(matcherFactoryMock.Object);

            var app = new ApplicationBuilder(services);

            // Act
            app.UseRouting(builder =>
            {
                builder.Map("/1", "Test endpoint 1", d => null);
                builder.Map("/2", "Test endpoint 2", d => null);
            });

            app.UseRouting(builder =>
            {
                builder.Map("/3", "Test endpoint 3", d => null);
                builder.Map("/4", "Test endpoint 4", d => null);
            });

            // This triggers the middleware to be created and the matcher factory to be called
            // with the datasource we want to test
            var requestDelegate = app.Build();
            requestDelegate(new DefaultHttpContext());

            // Assert
            Assert.Equal(2, matcherEndpointDataSources.Count);

            // Each middleware has its own endpoints
            Assert.Collection(matcherEndpointDataSources[0].Endpoints,
                e => Assert.Equal("Test endpoint 1", e.DisplayName),
                e => Assert.Equal("Test endpoint 2", e.DisplayName));
            Assert.Collection(matcherEndpointDataSources[1].Endpoints,
                e => Assert.Equal("Test endpoint 3", e.DisplayName),
                e => Assert.Equal("Test endpoint 4", e.DisplayName));

            var compositeEndpointBuilder = services.GetRequiredService<EndpointDataSource>();

            // Global middleware has all endpoints
            Assert.Collection(compositeEndpointBuilder.Endpoints,
                e => Assert.Equal("Test endpoint 1", e.DisplayName),
                e => Assert.Equal("Test endpoint 2", e.DisplayName),
                e => Assert.Equal("Test endpoint 3", e.DisplayName),
                e => Assert.Equal("Test endpoint 4", e.DisplayName));
        }

        private IServiceProvider CreateServices()
        {
            return CreateServices(matcherFactory: null);
        }

        private IServiceProvider CreateServices(MatcherFactory matcherFactory)
        {
            var services = new ServiceCollection();

            if (matcherFactory != null)
            {
                services.AddSingleton<MatcherFactory>(matcherFactory);
            }

            services.AddLogging();
            services.AddOptions();
            services.AddRouting();

            var serviceProvder = services.BuildServiceProvider();

            return serviceProvder;
        }
    }
}
