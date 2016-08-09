// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class MiddlewareFilterBuilderTest
    {
        [Fact]
        public void GetPipeline_CallsInto_Configure()
        {
            // Arrange
            var services = new ServiceCollection();
            var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
            var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider());
            pipelineBuilderService.ApplicationBuilder = appBuilder;
            var configureCount = 0;
            Pipeline1.ConfigurePipeline = (ab) =>
            {
                configureCount++;
            };

            // Act
            var pipeline = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

            // Assert
            Assert.NotNull(pipeline);
            Assert.Equal(1, configureCount);
        }

        [Fact]
        public void GetPipeline_CallsIntoConfigure_OnlyOnce_ForTheSamePipelineType()
        {
            // Arrange
            var services = new ServiceCollection();
            var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
            var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider());
            pipelineBuilderService.ApplicationBuilder = appBuilder;
            var configureCount = 0;
            Pipeline1.ConfigurePipeline = (ab) =>
            {
                configureCount++;
            };

            // Act
            var pipeline1 = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

            // Assert
            Assert.NotNull(pipeline1);
            Assert.Equal(1, configureCount);

            // Act
            var pipeline2 = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

            // Assert
            Assert.NotNull(pipeline2);
            Assert.Same(pipeline1, pipeline2);
            Assert.Equal(1, configureCount);
        }

        [Fact]
        public async Task EndMiddleware_ThrowsException_WhenMiddleFeature_NotAvailable()
        {
            // Arrange
            var services = new ServiceCollection();
            var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
            var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider());
            pipelineBuilderService.ApplicationBuilder = appBuilder;
            Pipeline1.ConfigurePipeline = (ab) =>
            {
                ab.Use((httpContext, next) =>
                {
                    return next();
                });
            };

            // Act
            var pipeline = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

            // Assert
            Assert.NotNull(pipeline);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline(new DefaultHttpContext()));
            Assert.Equal(
                "Could not find 'IMiddlewareFilterFeature' in the feature list.",
                exception.Message);
        }

        [Fact]
        public async Task EndMiddleware_PropagatesBackException_ToEarlierMiddleware()
        {
            // Arrange
            var services = new ServiceCollection();
            var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
            var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider());
            pipelineBuilderService.ApplicationBuilder = appBuilder;
            Pipeline1.ConfigurePipeline = (ab) =>
            {
                ab.Use((httpCtxt, next) =>
                {
                    return next();
                });
            };
            var middlewareFilterFeature = new MiddlewareFilterFeature();
            middlewareFilterFeature.ResourceExecutionDelegate = () =>
            {
                var context = new ResourceExecutedContext(
                    new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary()),
                    new List<IFilterMetadata>());
                context.Exception = new InvalidOperationException("Error!!!");
                return Task.FromResult(context);
            };
            var features = new FeatureCollection();
            features.Set<IMiddlewareFilterFeature>(middlewareFilterFeature);
            var httpContext = new DefaultHttpContext(features);

            // Act
            var pipeline = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

            // Assert
            Assert.NotNull(pipeline);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline(httpContext));
            Assert.Equal("Error!!!", exception.Message);
        }
        private class Pipeline1
        {
            public static Action<IApplicationBuilder> ConfigurePipeline { get; set; }

            public void Configure(IApplicationBuilder appBuilder)
            {
                ConfigurePipeline(appBuilder);
            }
        }

        private class Pipeline2
        {
            public static Action<IApplicationBuilder> ConfigurePipeline { get; set; }

            public void Configure(IApplicationBuilder appBuilder)
            {
                ConfigurePipeline(appBuilder);
            }
        }
    }
}
