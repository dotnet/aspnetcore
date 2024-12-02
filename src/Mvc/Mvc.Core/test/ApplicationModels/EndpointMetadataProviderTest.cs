// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class EndpointMetadataProviderTest
{
    [Theory]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInValueTaskOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInValueTaskOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInTaskOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInTaskOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInFSharpAsyncOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInFSharpAsyncOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInActionResult))]
    public void DiscoversEndpointMetadata_FromReturnTypeImplementingIEndpointMetadataProvider(Type controllerType, string actionName)
    {
        // Act
        var endpoint = GetEndpoint(controllerType, actionName);

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public void DiscoversEndpointMetadata_ForAllSelectors_FromReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Act
        var endpoints = GetEndpoints(typeof(TestController), nameof(TestController.MultipleSelectorsActionWithMetadataInActionResult));

        // Assert
        Assert.Collection(endpoints,
            endpoint => Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType }),
            endpoint => Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType }));
    }

    [Fact]
    public void DiscoversMetadata_FromParametersImplementingIEndpointParameterMetadataProvider()
    {
        // Act
        var endpoint = GetEndpoint(typeof(TestController), nameof(TestController.ActionWithParameterMetadata));

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: "param1" });
    }

    [Fact]
    public void DiscoversEndpointMetadata_ForAllSelectors_FromParametersImplementingIEndpointParameterMetadataProvider()
    {
        // Act
        var endpoints = GetEndpoints(typeof(TestController), nameof(TestController.MultipleSelectorsActionWithParameterMetadata));

        // Assert
        Assert.Collection(endpoints,
            endpoint => Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: "param1" }),
            endpoint => Assert.Contains(endpoint.Metadata, m => m is ParameterNameMetadata { Name: "param1" }));
    }

    [Fact]
    public void DiscoversMetadata_FromParametersImplementingIEndpointMetadataProvider()
    {
        // Act
        var endpoint = GetEndpoint(typeof(TestController), nameof(TestController.ActionWithParameterMetadata));

        // Assert
        Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    }

    [Fact]
    public void DiscoversEndpointMetadata_ForAllSelectors_FromParametersImplementingIEndpointMetadataProvider()
    {
        // Act
        var endpoints = GetEndpoints(typeof(TestController), nameof(TestController.MultipleSelectorsActionWithParameterMetadata));

        // Assert
        Assert.Collection(endpoints,
            endpoint => Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter }),
            endpoint => Assert.Contains(endpoint.Metadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter }));
    }

    [Fact]
    public void DiscoversMetadata_CorrectOrder()
    {
        // Arrange
        var dataSource = GetEndpointDataSource(typeof(TestController), nameof(TestController.ActionWithParameterMetadata));
        var routeGroupContext = new RouteGroupContext
        {
            Prefix = RoutePatternFactory.Parse("/"),
            Conventions = new Action<EndpointBuilder>[]
            {
                builder => builder.Metadata.Add(new CustomEndpointMetadata() { Source = MetadataSource.Caller }),
            },
            FinallyConventions = new Action<EndpointBuilder>[]
            {
                builder => builder.Metadata.Add(new CustomEndpointMetadata() { Source = MetadataSource.Finally }),
            },
        };

        // Act
        var endpoint = Assert.Single(FilterEndpoints(dataSource.GetGroupedEndpoints(routeGroupContext)));

        // Assert
        Assert.Collection(
            endpoint.Metadata,
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Caller }),
            m => Assert.True(m is ParameterNameMetadata { Name: "param1" }),
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Parameter }),
            m => Assert.True(m is CustomAttribute),
            m => Assert.True(m is ControllerActionDescriptor),
            m => Assert.True(m is RouteNameMetadata),
            m => Assert.True(m is SuppressLinkGenerationMetadata),
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Finally }),
            m => Assert.True(m is IRouteDiagnosticsMetadata { Route: "/{controller}/{action}/{id?}" }));
    }

    [Theory]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInValueTaskOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInValueTaskOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInTaskOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInTaskOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInFSharpAsyncOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInFSharpAsyncOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInActionResult))]
    public void AllowsRemovalOfMetadata_ByReturnTypeImplementingIEndpointMetadataProvider(Type controllerType, string actionName)
    {
        // Arrange
        var dataSource = GetEndpointDataSource(controllerType, actionName);
        var routeGroupContext = new RouteGroupContext
        {
            Prefix = RoutePatternFactory.Parse("/"),
            Conventions = new Action<EndpointBuilder>[]
            {
                builder => builder.Metadata.Add(new ConsumesAttribute("application/json")),
            },
        };

        // Act
        var endpoint = Assert.Single(FilterEndpoints(dataSource.GetGroupedEndpoints(routeGroupContext)));

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void AllowsRemovalOfMetadata_ByParameterTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var dataSource = GetEndpointDataSource(typeof(TestController), nameof(TestController.ActionWithRemovalFromParameterEndpointMetadata));
        var routeGroupContext = new RouteGroupContext
        {
            Prefix = RoutePatternFactory.Parse("/"),
            Conventions = new Action<EndpointBuilder>[]
            {
                builder => builder.Metadata.Add(new ConsumesAttribute("application/json")),
            },
        };

        //Act
        var endpoint = Assert.Single(FilterEndpoints(dataSource.GetGroupedEndpoints(routeGroupContext)));

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void AllowsRemovalOfMetadata_ByParameterTypeImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var dataSource = GetEndpointDataSource(typeof(TestController), nameof(TestController.ActionWithRemovalFromParameterMetadata));
        var routeGroupContext = new RouteGroupContext
        {
            Prefix = RoutePatternFactory.Parse("/"),
            Conventions = new Action<EndpointBuilder>[]
            {
                builder => builder.Metadata.Add(new ConsumesAttribute("application/json")),
            },
        };

        // Act
        var endpoint = Assert.Single(FilterEndpoints(dataSource.GetGroupedEndpoints(routeGroupContext)));

        // Assert
        Assert.DoesNotContain(endpoint.Metadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void CanObserveRoutePattern_ForAllSelectors_FromParameterImplementingIEndpointetadataProvider()
    {
        // Act
        var endpoints = GetEndpoints(typeof(TestController), nameof(TestController.MultipleSelectorsActionWithRoutePatternMetadata));

        // Assert
        Assert.Collection(endpoints,
            endpoint => Assert.Contains(endpoint.Metadata, m => m is RoutePatternMetadata { RoutePattern: "selector1" }),
            endpoint => Assert.Contains(endpoint.Metadata, m => m is RoutePatternMetadata { RoutePattern: "selector2" }));
    }

    private Endpoint GetEndpoint(Type controllerType, string actionName) => Assert.Single(GetEndpoints(controllerType, actionName));
    private List<Endpoint> GetEndpoints(Type controllerType, string actionName) => FilterEndpoints(GetEndpointDataSource(controllerType, actionName).Endpoints);

    // Filter out duplicate endpoints created by AddConventionalLinkGenerationRoute.
    // These are added per route defined by MapControllerRoute rather than per action, so do not have inferred metadata.
    private List<Endpoint> FilterEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        var nonLinkGenerationEndpoints = new List<Endpoint>();

        foreach (var endpoint in endpoints)
        {
            if (endpoint.Metadata is not [SuppressMatchingMetadata, ..])
            {
                nonLinkGenerationEndpoints.Add(endpoint);
            }
        }

        return nonLinkGenerationEndpoints;
    }

    private ControllerActionEndpointDataSource GetEndpointDataSource(Type controllerType, string actionName)
    {
        // Create ActionDescriptors how we normally would by default for the given controllerType
        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new TestApplicationPart(controllerType));
        manager.FeatureProviders.Add(new TestFeatureProvider());

        var options = Options.Create(new MvcOptions());
        var modelProvider = new DefaultApplicationModelProvider(options, new EmptyModelMetadataProvider());
        var controllerActionDescriptorProvider = new ControllerActionDescriptorProvider(
            manager,
            new ApplicationModelFactory(new[] { modelProvider }, options));

        var actionDescriptorProviderContext = new ActionDescriptorProviderContext();
        controllerActionDescriptorProvider.OnProvidersExecuting(actionDescriptorProviderContext);
        controllerActionDescriptorProvider.OnProvidersExecuted(actionDescriptorProviderContext);

        // Filter the ActionDescriptors by action name.
        var descriptorsWithMatchingActionName = new List<ControllerActionDescriptor>();

        foreach (var descriptor in actionDescriptorProviderContext.Results)
        {
            if (descriptor is ControllerActionDescriptor cad &&
                cad.MethodInfo.Name == actionName)
            {
                descriptorsWithMatchingActionName.Add(cad);
            }
        }

        // Configure the ControllerActionEndpointDataSource to use our filtered ActionDescriptors for endpoint generation.
        var actions = new MockActionDescriptorCollectionProvider(descriptorsWithMatchingActionName);

        var services = new ServiceCollection();
        services.AddSingleton(actions);

        var routeOptionsSetup = new MvcCoreRouteOptionsSetup();
        services.Configure<RouteOptions>(routeOptionsSetup.Configure);
        services.AddRouting();
        var serviceProvider = services.BuildServiceProvider();

        var endpointFactory = new ActionEndpointFactory(serviceProvider.GetRequiredService<RoutePatternTransformer>(), Enumerable.Empty<IRequestDelegateFactory>(), serviceProvider);

        var dataSource = new ControllerActionEndpointDataSource(
            new ControllerActionEndpointDataSourceIdProvider(),
            actions,
            endpointFactory,
            new OrderedEndpointsSequenceProvider());

        // Add single route for non-attribute-routed actions.
        dataSource.AddRoute("default", "/{controller}/{action}/{id?}", null, null, null);

        return dataSource;
    }

    private sealed class MockActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
    {
        public MockActionDescriptorCollectionProvider(IReadOnlyList<ActionDescriptor> actions)
        {
            ActionDescriptors = new ActionDescriptorCollection(actions, 0);
        }

        public ActionDescriptorCollection ActionDescriptors { get; }
    }

    private class TestController
    {
        [Custom]
        public ActionResult ActionWithParameterMetadata(AddsCustomParameterMetadata param1) => null;
        public ActionResult ActionWithRemovalFromParameterMetadata(RemovesAcceptsParameterMetadata param1) => null;
        public ActionResult ActionWithRemovalFromParameterEndpointMetadata(RemovesAcceptsParameterEndpointMetadata param1) => null;

        [HttpGet("selector1")]
        [HttpGet("selector2")]
        public ActionResult MultipleSelectorsActionWithParameterMetadata(AddsCustomParameterMetadata param1) => null;

        [HttpGet("selector1")]
        [HttpGet("selector2")]
        public ActionResult MultipleSelectorsActionWithRoutePatternMetadata(AddsRoutePatternMetadata param1) => null;

        public AddsCustomEndpointMetadataResult ActionWithMetadataInResult() => null;

        public ValueTask<AddsCustomEndpointMetadataResult> ActionWithMetadataInValueTaskOfResult()
            => ValueTask.FromResult<AddsCustomEndpointMetadataResult>(null);

        public Task<AddsCustomEndpointMetadataResult> ActionWithMetadataInTaskOfResult()
            => Task.FromResult<AddsCustomEndpointMetadataResult>(null);

        public FSharp.Control.FSharpAsync<AddsCustomEndpointMetadataResult> ActionWithMetadataInFSharpAsyncOfResult()
            => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return<AddsCustomEndpointMetadataResult>(null);

        [HttpGet("selector1")]
        [HttpGet("selector2")]
        public AddsCustomEndpointMetadataActionResult MultipleSelectorsActionWithMetadataInActionResult() => null;

        public AddsCustomEndpointMetadataActionResult ActionWithMetadataInActionResult() => null;

        public ValueTask<AddsCustomEndpointMetadataActionResult> ActionWithMetadataInValueTaskOfActionResult()
            => ValueTask.FromResult<AddsCustomEndpointMetadataActionResult>(null);

        public Task<AddsCustomEndpointMetadataActionResult> ActionWithMetadataInTaskOfActionResult()
            => Task.FromResult<AddsCustomEndpointMetadataActionResult>(null);

        public FSharp.Control.FSharpAsync<AddsCustomEndpointMetadataActionResult> ActionWithMetadataInFSharpAsyncOfActionResult()
            => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return<AddsCustomEndpointMetadataActionResult>(null);

        public RemovesAcceptsMetadataResult ActionWithNoAcceptsMetadataInResult() => null;

        public ValueTask<RemovesAcceptsMetadataResult> ActionWithNoAcceptsMetadataInValueTaskOfResult()
            => ValueTask.FromResult<RemovesAcceptsMetadataResult>(null);

        public Task<RemovesAcceptsMetadataResult> ActionWithNoAcceptsMetadataInTaskOfResult()
            => Task.FromResult<RemovesAcceptsMetadataResult>(null);

        public FSharp.Control.FSharpAsync<RemovesAcceptsMetadataResult> ActionWithNoAcceptsMetadataInFSharpAsyncOfResult()
            => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return<RemovesAcceptsMetadataResult>(null);

        public RemovesAcceptsMetadataActionResult ActionWithNoAcceptsMetadataInActionResult() => null;

        public ValueTask<RemovesAcceptsMetadataActionResult> ActionWithNoAcceptsMetadataInValueTaskOfActionResult()
            => ValueTask.FromResult<RemovesAcceptsMetadataActionResult>(null);

        public Task<RemovesAcceptsMetadataActionResult> ActionWithNoAcceptsMetadataInTaskOfActionResult()
            => Task.FromResult<RemovesAcceptsMetadataActionResult>(null);

        public FSharp.Control.FSharpAsync<RemovesAcceptsMetadataActionResult> ActionWithNoAcceptsMetadataInFSharpAsyncOfActionResult()
            => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return<RemovesAcceptsMetadataActionResult>(null);
    }

    private class CustomEndpointMetadata
    {
        public string Data { get; init; }

        public MetadataSource Source { get; init; }
    }

    private class ParameterNameMetadata
    {
        public string Name { get; init; }
    }

    private class RoutePatternMetadata
    {
        public string RoutePattern { get; init; }
    }

    private class CustomAttribute : Attribute
    {
    }

    private enum MetadataSource
    {
        Caller,
        Parameter,
        ReturnType,
        Finally
    }

    private class AddsCustomParameterMetadata : IEndpointParameterMetadataProvider, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
        {
            builder.Metadata.Add(new ParameterNameMetadata { Name = parameter.Name });
        }

        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Parameter });
        }
    }

    private class AddsCustomEndpointMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.ReturnType });
        }

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }

    private class AddsCustomEndpointMetadataActionResult : IEndpointMetadataProvider, IActionResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.ReturnType });
        }
        public Task ExecuteResultAsync(ActionContext context) => throw new NotImplementedException();
    }

    private class AddsRoutePatternMetadata : IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            if (builder is not RouteEndpointBuilder reb)
            {
                return;
            }

            builder.Metadata.Add(new RoutePatternMetadata { RoutePattern = reb.RoutePattern.RawText });
        }
    }

    private class RemovesAcceptsMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            for (int i = builder.Metadata.Count - 1; i >= 0; i--)
            {
                var metadata = builder.Metadata[i];
                if (metadata is IAcceptsMetadata)
                {
                    builder.Metadata.RemoveAt(i);
                }
            }
        }

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }

    private class RemovesAcceptsMetadataActionResult : IEndpointMetadataProvider, IActionResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            if (builder.Metadata is not null)
            {
                for (int i = builder.Metadata.Count - 1; i >= 0; i--)
                {
                    var metadata = builder.Metadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        builder.Metadata.RemoveAt(i);
                    }
                }
            }
        }

        public Task ExecuteResultAsync(ActionContext context) => throw new NotImplementedException();
    }

    private class RemovesAcceptsParameterMetadata : IEndpointParameterMetadataProvider
    {
        public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
        {
            if (builder.Metadata is not null)
            {
                for (int i = builder.Metadata.Count - 1; i >= 0; i--)
                {
                    var metadata = builder.Metadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        builder.Metadata.RemoveAt(i);
                    }
                }
            }
        }
    }

    private class RemovesAcceptsParameterEndpointMetadata : IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            if (builder.Metadata is not null)
            {
                for (int i = builder.Metadata.Count - 1; i >= 0; i--)
                {
                    var metadata = builder.Metadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        builder.Metadata.RemoveAt(i);
                    }
                }
            }
        }
    }
}
