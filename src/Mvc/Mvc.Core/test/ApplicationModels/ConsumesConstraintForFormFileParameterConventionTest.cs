// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class ConsumesConstraintForFormFileParameterConventionTest
{
    [Fact]
    public void AddMultipartFormDataConsumesAttribute_NoOpsIfConsumesConstraintIsAlreadyPresent()
    {
        // Arrange
        var actionName = nameof(TestController.ActionWithConsumesAttribute);
        var action = GetActionModel(typeof(TestController), actionName);
        var convention = GetConvention();

        // Act
        ConsumesConstraintForFormFileParameterConvention.AddMultipartFormDataConsumesAttribute(action);

        // Assert
        var attribute = Assert.Single(action.Filters);
        var consumesAttribute = Assert.IsType<ConsumesAttribute>(attribute);
        Assert.Equal("application/json", Assert.Single(consumesAttribute.ContentTypes));
    }

    [Fact]
    public void AddMultipartFormDataConsumesAttribute_AddsConsumesAttribute_WhenActionHasFromFormFileParameter()
    {
        // Arrange
        var actionName = nameof(TestController.FormFileParameter);
        var action = GetActionModel(typeof(TestController), actionName);
        action.Parameters[0].BindingInfo = new BindingInfo
        {
            BindingSource = BindingSource.FormFile,
        };
        var convention = GetConvention();

        // Act
        ConsumesConstraintForFormFileParameterConvention.AddMultipartFormDataConsumesAttribute(action);

        // Assert
        var attribute = Assert.Single(action.Filters);
        var consumesAttribute = Assert.IsType<ConsumesAttribute>(attribute);
        Assert.Equal("multipart/form-data", Assert.Single(consumesAttribute.ContentTypes));
    }

    private ConsumesConstraintForFormFileParameterConvention GetConvention()
    {
        return new ConsumesConstraintForFormFileParameterConvention();
    }

    private static ApplicationModelProviderContext GetContext(
        Type type,
        IModelMetadataProvider modelMetadataProvider = null)
    {
        var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
        var mvcOptions = Options.Create(new MvcOptions());
        modelMetadataProvider = modelMetadataProvider ?? new EmptyModelMetadataProvider();
        var convention = new DefaultApplicationModelProvider(mvcOptions, modelMetadataProvider);
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
        [HttpPost("form-file")]
        public IActionResult FormFileParameter(IFormFile formFile) => null;

        [HttpPost("form-file-collection")]
        public IActionResult FormFileCollectionParameter(IFormFileCollection formFiles) => null;

        [HttpPost("form-file-sequence")]
        public IActionResult FormFileSequenceParameter(IFormFile[] formFiles) => null;

        [HttpPost]
        public IActionResult FromFormParameter([FromForm] string parameter) => null;

        [HttpPost]
        [Consumes("application/json")]
        public IActionResult ActionWithConsumesAttribute([FromForm] string parameter) => null;
    }
}
