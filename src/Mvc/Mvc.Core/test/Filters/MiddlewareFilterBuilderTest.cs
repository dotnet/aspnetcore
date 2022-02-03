// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class MiddlewareFilterBuilderTest
{
    [Fact]
    public void GetPipeline_CallsInto_Configure()
    {
        // Arrange
        var services = new ServiceCollection();
        var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
        var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider())
        {
            ApplicationBuilder = appBuilder,
        };

        var configureCount = 0;
        Pipeline1.ConfigurePipeline = _ =>
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
        var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider())
        {
            ApplicationBuilder = appBuilder,
        };

        var configureCount = 0;
        Pipeline1.ConfigurePipeline = _ =>
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
        var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider())
        {
            ApplicationBuilder = appBuilder,
        };

        var httpContext = new DefaultHttpContext();
        Pipeline1.ConfigurePipeline = ab =>
        {
            ab.Use((ctx, next) =>
            {
                return next(ctx);
            });
        };

        // Act
        var pipeline = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

        // Assert
        Assert.NotNull(pipeline);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline(httpContext));
        Assert.Equal($"Feature '{typeof(IMiddlewareFilterFeature)}' is not present.", exception.Message);
    }

    [Fact]
    public async Task EndMiddleware_DoesNotThrow_IfExceptionHandled()
    {
        // Arrange
        var services = new ServiceCollection();
        var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
        var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider())
        {
            ApplicationBuilder = appBuilder,
        };

        Pipeline1.ConfigurePipeline = ab =>
        {
            ab.Use((ctx, next) =>
            {
                return next(ctx);
            });
        };

        var middlewareFilterFeature = new MiddlewareFilterFeature
        {
            ResourceExecutionDelegate = () =>
            {
                var actionContext = new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ActionDescriptor(),
                    new ModelStateDictionary());
                var context = new ResourceExecutedContext(actionContext, new List<IFilterMetadata>())
                {
                    Exception = new InvalidOperationException("Error!!!"),
                    ExceptionHandled = true,
                };

                return Task.FromResult(context);
            },
        };

        var features = new FeatureCollection();
        features.Set<IMiddlewareFilterFeature>(middlewareFilterFeature);
        var httpContext = new DefaultHttpContext(features);

        // Act
        var pipeline = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

        // Assert
        Assert.NotNull(pipeline);

        // Does not throw.
        await pipeline(httpContext);
    }

    [Fact]
    public async Task EndMiddleware_PropagatesBackException_ToEarlierMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
        var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider())
        {
            ApplicationBuilder = appBuilder,
        };

        Pipeline1.ConfigurePipeline = ab =>
        {
            ab.Use((ctx, next) =>
            {
                return next(ctx);
            });
        };

        var middlewareFilterFeature = new MiddlewareFilterFeature
        {
            ResourceExecutionDelegate = () =>
            {
                Exception thrownException;
                try
                {
                    // Create a small stack trace.
                    throw new InvalidOperationException("Error!!!");
                }
                catch (Exception ex)
                {
                    thrownException = ex;
                }

                var actionContext = new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ActionDescriptor(),
                    new ModelStateDictionary());
                var context = new ResourceExecutedContext(actionContext, new List<IFilterMetadata>())
                {
                    Exception = thrownException,
                };

                return Task.FromResult(context);
            },
        };

        var features = new FeatureCollection();
        features.Set<IMiddlewareFilterFeature>(middlewareFilterFeature);
        var httpContext = new DefaultHttpContext(features);

        // Act
        var pipeline = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

        // Assert
        Assert.NotNull(pipeline);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline(httpContext));
        Assert.Null(exception.InnerException);
        Assert.Equal("Error!!!", exception.Message);

        var stack = exception.StackTrace;
        Assert.Contains(typeof(MiddlewareFilterBuilder).FullName, stack);
        Assert.DoesNotContain(typeof(MiddlewareFilterBuilderTest).FullName, stack);
        Assert.DoesNotContain(nameof(EndMiddleware_PropagatesBackException_ToEarlierMiddleware), stack);
    }

    [Fact]
    public async Task EndMiddleware_PropagatesFullExceptionInfo_ToEarlierMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
        var pipelineBuilderService = new MiddlewareFilterBuilder(new MiddlewareFilterConfigurationProvider())
        {
            ApplicationBuilder = appBuilder,
        };

        Pipeline1.ConfigurePipeline = ab =>
        {
            ab.Use((ctx, next) =>
            {
                return next(ctx);
            });
        };

        var middlewareFilterFeature = new MiddlewareFilterFeature
        {
            ResourceExecutionDelegate = () =>
            {
                ExceptionDispatchInfo exceptionInfo;
                try
                {
                    // Create a small stack trace.
                    throw new InvalidOperationException("Error!!!");
                }
                catch (Exception ex)
                {
                    exceptionInfo = ExceptionDispatchInfo.Capture(ex);
                }

                var actionContext = new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ActionDescriptor(),
                    new ModelStateDictionary());
                var context = new ResourceExecutedContext(actionContext, new List<IFilterMetadata>())
                {
                    ExceptionDispatchInfo = exceptionInfo,
                };

                return Task.FromResult(context);
            },
        };

        var features = new FeatureCollection();
        features.Set<IMiddlewareFilterFeature>(middlewareFilterFeature);
        var httpContext = new DefaultHttpContext(features);

        // Act
        var pipeline = pipelineBuilderService.GetPipeline(typeof(Pipeline1));

        // Assert
        Assert.NotNull(pipeline);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline(httpContext));
        Assert.Null(exception.InnerException);
        Assert.Equal("Error!!!", exception.Message);

        var stack = exception.StackTrace;
        Assert.Contains(typeof(MiddlewareFilterBuilder).FullName, stack);
        Assert.Contains(typeof(MiddlewareFilterBuilderTest).FullName, stack);
        Assert.Contains(nameof(EndMiddleware_PropagatesFullExceptionInfo_ToEarlierMiddleware), stack);
    }

    private class Pipeline1
    {
        public static Action<IApplicationBuilder> ConfigurePipeline { get; set; }

        public void Configure(IApplicationBuilder appBuilder)
        {
            ConfigurePipeline(appBuilder);
        }
    }
}
