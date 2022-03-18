// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class HttpResultMetadataConventionTest
{
    [Fact]
    public void Apply_AddsFilter()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.IResultAction));
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        Assert.Single(action.Filters.OfType<IApiResponseMetadataProvider>());
    }

    [Fact]
    public void Apply_DoesNotAddFilter_ForActionResult()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.IActionResultAction));
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        Assert.Empty(action.Filters.OfType<IApiResponseMetadataProvider>());
    }

    [Fact]
    public void Apply_DoesNotAddFilter_ForUserDefinedType()
    {
        // Arrange
        var action = GetActionModel(typeof(TestController), nameof(TestController.UserDefinedTypeAction));
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        Assert.Empty(action.Filters.OfType<IApiResponseMetadataProvider>());
    }

    private HttpResultMetadataConvention GetConvention() => new HttpResultMetadataConvention();

    private static ApplicationModelProviderContext GetContext(Type type)
    {
        var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
        var convention = new DefaultApplicationModelProvider(Options.Create(new MvcOptions()), new EmptyModelMetadataProvider());
        convention.OnProvidersExecuting(context);

        return context;
    }

    private static ActionModel GetActionModel(Type controllerType, string actionName)
    {
        var context = GetContext(controllerType);
        var controller = Assert.Single(context.Result.Controllers);
        return Assert.Single(controller.Actions, m => m.ActionName == actionName);
    }

    private class TestController
    {
        public record Todo(int id, string title);

        public IResult IResultAction(object value) => null;
        public IActionResult IActionResultAction(object value) => null;
        public Todo UserDefinedTypeAction(object value) => default(Todo);
    }
}
