// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class EndpointMetadataConventionTest
{
    [Theory]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInValueTaskOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInValueTaskOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInTaskOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInTaskOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithMetadataInActionResult))]
    public void Apply_DiscoversEndpointMetadata_FromReturnTypeImplementingIEndpointMetadataProvider(
        Type controllerType,
        string actionName)
    {
        // Arrange
        var action = GetActionModel(controllerType, actionName);
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        Assert.Contains(action.Selectors[0].EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public void Apply_DiscoversEndpointMetadata_ForAllSelectors_FromReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.MultipleSelectorsActionWithMetadataInActionResult));
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        foreach (var selector in action.Selectors)
        {
            Assert.Contains(selector.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
        }
    }

    [Fact]
    public void Apply_DiscoversMetadata_FromParametersImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.ActionWithParameterMetadata));
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        Assert.Contains(action.Selectors[0].EndpointMetadata, m => m is ParameterNameMetadata { Name: "param1" });
    }

    [Fact]
    public void Apply_DiscoversEndpointMetadata_ForAllSelectors_FromParametersImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.MultipleSelectorsActionWithParameterMetadata));
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        foreach (var selector in action.Selectors)
        {
            Assert.Contains(selector.EndpointMetadata, m => m is ParameterNameMetadata { Name: "param1" });
        }
    }

    [Fact]
    public void Apply_DiscoversMetadata_FromParametersImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.ActionWithParameterMetadata));
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        Assert.Contains(action.Selectors[0].EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    }

    [Fact]
    public void Apply_DiscoversEndpointMetadata_ForAllSelectors_FromParametersImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.MultipleSelectorsActionWithParameterMetadata));
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        foreach (var selector in action.Selectors)
        {
            Assert.Contains(selector.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
        }
    }

    [Fact]
    public void Apply_DiscoversMetadata_CorrectOrder()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.ActionWithParameterMetadata));
        action.Selectors[0].EndpointMetadata.Add(new CustomEndpointMetadata() {  Source = MetadataSource.Caller });
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        Assert.Collection(
            action.Selectors[0].EndpointMetadata,
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Caller }),
            m => Assert.True(m is ParameterNameMetadata { Name: "param1" }),
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Parameter }));
    }

    [Theory]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInValueTaskOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInValueTaskOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInTaskOfResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInTaskOfActionResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInResult))]
    [InlineData(typeof(TestController), nameof(TestController.ActionWithNoAcceptsMetadataInActionResult))]
    public void Apply_AllowsRemovalOfMetadata_ByReturnTypeImplementingIEndpointMetadataProvider(
        Type controllerType,
        string actionName)
    {
        // Arrange
        var action = GetActionModel(controllerType, actionName);
        action.Selectors[0].EndpointMetadata.Add(new ConsumesAttribute("application/json"));
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        Assert.DoesNotContain(action.Selectors[0].EndpointMetadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void Apply_AllowsRemovalOfMetadata_ByParameterTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.ActionWithRemovalFromParameterEndpointMetadata));
        action.Selectors[0].EndpointMetadata.Add(new ConsumesAttribute("application/json"));
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        Assert.DoesNotContain(action.Selectors[0].EndpointMetadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void Apply_AllowsRemovalOfMetadata_ByParameterTypeImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.ActionWithRemovalFromParameterMetadata));
        action.Selectors[0].EndpointMetadata.Add(new ConsumesAttribute("application/json"));
        var convention = GetConvention();

        //Act
        convention.Apply(action);

        // Assert
        Assert.DoesNotContain(action.Selectors[0].EndpointMetadata, m => m is IAcceptsMetadata);
    }

    private static EndpointMetadataConvention GetConvention(IServiceProvider services = null)
    {
        services ??= Mock.Of<IServiceProvider>();
        return new EndpointMetadataConvention(services);
    }

    private static ApplicationModelProviderContext GetContext(Type type)
    {
        var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
        var mvcOptions = Options.Create(new MvcOptions());
        var convention = new DefaultApplicationModelProvider(mvcOptions, new EmptyModelMetadataProvider());
        convention.OnProvidersExecuting(context);

        return context;
    }

    private static ActionModel GetActionModel(
        Type controllerType,
        string actionName)
    {
        var context = GetContext(controllerType);
        var controller = Assert.Single(context.Result.Controllers);
        return Assert.Single(controller.Actions, m => m.ActionName == actionName);
    }

    private class TestController
    {
        public ActionResult ActionWithParameterMetadata(AddsCustomParameterMetadata param1) => null;
        public ActionResult ActionWithRemovalFromParameterMetadata(RemovesAcceptsParameterMetadata param1) => null;
        public ActionResult ActionWithRemovalFromParameterEndpointMetadata(RemovesAcceptsParameterEndpointMetadata param1) => null;

        [HttpGet("selector1")]
        [HttpGet("selector2")]
        public ActionResult MultipleSelectorsActionWithParameterMetadata(AddsCustomParameterMetadata param1) => null;

        public AddsCustomEndpointMetadataResult ActionWithMetadataInResult() => null;

        public ValueTask<AddsCustomEndpointMetadataResult> ActionWithMetadataInValueTaskOfResult()
            => ValueTask.FromResult<AddsCustomEndpointMetadataResult>(null);

        public Task<AddsCustomEndpointMetadataResult> ActionWithMetadataInTaskOfResult()
            => Task.FromResult<AddsCustomEndpointMetadataResult>(null);

        [HttpGet("selector1")]
        [HttpGet("selector2")]
        public AddsCustomEndpointMetadataActionResult MultipleSelectorsActionWithMetadataInActionResult() => null;

        public AddsCustomEndpointMetadataActionResult ActionWithMetadataInActionResult() => null;

        public ValueTask<AddsCustomEndpointMetadataActionResult> ActionWithMetadataInValueTaskOfActionResult()
            => ValueTask.FromResult<AddsCustomEndpointMetadataActionResult>(null);

        public Task<AddsCustomEndpointMetadataActionResult> ActionWithMetadataInTaskOfActionResult()
            => Task.FromResult<AddsCustomEndpointMetadataActionResult>(null);

        public RemovesAcceptsMetadataResult ActionWithNoAcceptsMetadataInResult() => null;

        public ValueTask<RemovesAcceptsMetadataResult> ActionWithNoAcceptsMetadataInValueTaskOfResult()
            => ValueTask.FromResult<RemovesAcceptsMetadataResult>(null);

        public Task<RemovesAcceptsMetadataResult> ActionWithNoAcceptsMetadataInTaskOfResult()
            => Task.FromResult<RemovesAcceptsMetadataResult>(null);

        public RemovesAcceptsMetadataActionResult ActionWithNoAcceptsMetadataInActionResult() => null;

        public ValueTask<RemovesAcceptsMetadataActionResult> ActionWithNoAcceptsMetadataInValueTaskOfActionResult()
            => ValueTask.FromResult<RemovesAcceptsMetadataActionResult>(null);

        public Task<RemovesAcceptsMetadataActionResult> ActionWithNoAcceptsMetadataInTaskOfActionResult()
            => Task.FromResult<RemovesAcceptsMetadataActionResult>(null);
    }

    private class CustomEndpointMetadata
    {
        public string Data { get; init; }

        public MetadataSource Source { get; init; }
    }
    private enum MetadataSource
    {
        Caller,
        Parameter,
        ReturnType
    }

    private class ParameterNameMetadata
    {
        public string Name { get; init; }
    }

    private class AddsCustomParameterMetadata : IEndpointParameterMetadataProvider, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(EndpointParameterMetadataContext parameterContext)
        {
            parameterContext.EndpointMetadata?.Add(new ParameterNameMetadata { Name = parameterContext.Parameter?.Name });
        }

        public static void PopulateMetadata(EndpointMetadataContext context)
        {
            context.EndpointMetadata?.Add(new CustomEndpointMetadata { Source = MetadataSource.Parameter });
        }
    }

    private class AddsCustomEndpointMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(EndpointMetadataContext context)
        {
            context.EndpointMetadata?.Add(new CustomEndpointMetadata { Source = MetadataSource.ReturnType });
        }

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }

    private class AddsCustomEndpointMetadataActionResult : IEndpointMetadataProvider, IActionResult
    {
        public static void PopulateMetadata(EndpointMetadataContext context)
        {
            context.EndpointMetadata?.Add(new CustomEndpointMetadata { Source = MetadataSource.ReturnType });
        }
        public Task ExecuteResultAsync(ActionContext context) => throw new NotImplementedException();
    }

    private class RemovesAcceptsMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(EndpointMetadataContext context)
        {
            if (context.EndpointMetadata is not null)
            {
                for (int i = context.EndpointMetadata.Count - 1; i >= 0; i--)
                {
                    var metadata = context.EndpointMetadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        context.EndpointMetadata.RemoveAt(i);
                    }
                }
            }
        }

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }

    private class RemovesAcceptsMetadataActionResult : IEndpointMetadataProvider, IActionResult
    {
        public static void PopulateMetadata(EndpointMetadataContext context)
        {
            if (context.EndpointMetadata is not null)
            {
                for (int i = context.EndpointMetadata.Count - 1; i >= 0; i--)
                {
                    var metadata = context.EndpointMetadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        context.EndpointMetadata.RemoveAt(i);
                    }
                }
            }
        }

        public Task ExecuteResultAsync(ActionContext context) => throw new NotImplementedException();
    }

    private class RemovesAcceptsParameterMetadata : IEndpointParameterMetadataProvider
    {
        public static void PopulateMetadata(EndpointParameterMetadataContext parameterContext)
        {
            if (parameterContext.EndpointMetadata is not null)
            {
                for (int i = parameterContext.EndpointMetadata.Count - 1; i >= 0; i--)
                {
                    var metadata = parameterContext.EndpointMetadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        parameterContext.EndpointMetadata.RemoveAt(i);
                    }
                }
            }
        }
    }

    private class RemovesAcceptsParameterEndpointMetadata : IEndpointMetadataProvider
    {
        public static void PopulateMetadata(EndpointMetadataContext context)
        {
            if (context.EndpointMetadata is not null)
            {
                for (int i = context.EndpointMetadata.Count - 1; i >= 0; i--)
                {
                    var metadata = context.EndpointMetadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        context.EndpointMetadata.RemoveAt(i);
                    }
                }
            }
        }
    }
}
