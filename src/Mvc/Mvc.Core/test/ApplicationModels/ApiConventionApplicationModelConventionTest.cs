// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Authorization;

[assembly: ProducesErrorResponseType(typeof(InvalidEnumArgumentException))]

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class ApiConventionApplicationModelConventionTest
{
    [Fact]
    public void Apply_DoesNotAddConventionItem_IfNoConventionMatches()
    {
        // Arrange
        var actionModel = GetActionModel(nameof(TestController.NoMatch));
        var convention = GetConvention();

        // Act
        convention.Apply(actionModel);

        // Assert
        Assert.DoesNotContain(typeof(ApiConventionResult), actionModel.Properties.Keys);
    }

    [Fact]
    public void Apply_AddsConventionItem_IfConventionMatches()
    {
        // Arrange
        var actionModel = GetActionModel(nameof(TestController.Delete));
        var convention = GetConvention();

        // Act
        convention.Apply(actionModel);

        // Assert
        var value = actionModel.Properties[typeof(ApiConventionResult)];
        Assert.NotNull(value);
    }

    [Fact]
    public void Apply_AddsConventionItem_IfActionHasNonConventionBasedFilters()
    {
        // Arrange
        var actionModel = GetActionModel(nameof(TestController.Delete));
        actionModel.Filters.Add(new AuthorizeFilter());
        var convention = GetConvention();

        // Act
        convention.Apply(actionModel);

        // Assert
        var value = actionModel.Properties[typeof(ApiConventionResult)];
        Assert.NotNull(value);
    }

    [Fact]
    public void Apply_UsesDefaultErrorType_IfActionHasNoAttributes()
    {
        // Arrange
        var expected = typeof(InvalidFilterCriteriaException);
        var controller = new ControllerModel(typeof(object).GetTypeInfo(), Array.Empty<object>());
        var action = new ActionModel(typeof(object).GetMethods()[0], Array.Empty<object>())
        {
            Controller = controller,
        };
        var convention = GetConvention(expected);

        // Act
        convention.Apply(action);

        // Assert
        var attribute = GetProperty<ProducesErrorResponseTypeAttribute>(action);
        Assert.Equal(expected, attribute.Type);
    }

    [Fact]
    public void Apply_UsesValueFromProducesErrorResponseTypeAttribute_SpecifiedOnControllerAsssembly()
    {
        // Arrange
        var expected = typeof(InvalidEnumArgumentException);
        var action = GetActionModel(nameof(TestController.Delete));
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        var attribute = GetProperty<ProducesErrorResponseTypeAttribute>(action);
        Assert.Equal(expected, attribute.Type);
    }

    [Fact]
    public void Apply_UsesValueFromProducesErrorResponseTypeAttribute_SpecifiedOnController()
    {
        // Arrange
        var expected = typeof(InvalidTimeZoneException);
        var action = GetActionModel(
            nameof(TestController.Delete),
            controllerAttributes: new[] { new ProducesErrorResponseTypeAttribute(expected) });
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        var attribute = GetProperty<ProducesErrorResponseTypeAttribute>(action);
        Assert.Equal(expected, attribute.Type);
    }

    [Fact]
    public void Apply_UsesValueFromProducesErrorResponseTypeAttribute_SpecifiedOnAction()
    {
        // Arrange
        var expected = typeof(InvalidTimeZoneException);
        var action = GetActionModel(
            nameof(TestController.Delete),
            actionAttributes: new[] { new ProducesErrorResponseTypeAttribute(expected) },
            controllerAttributes: new[] { new ProducesErrorResponseTypeAttribute(typeof(Guid)) });
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        var attribute = GetProperty<ProducesErrorResponseTypeAttribute>(action);
        Assert.Equal(expected, attribute.Type);
    }

    [Fact]
    public void Apply_AllowsVoidsErrorType()
    {
        // Arrange
        var expected = typeof(void);
        var action = GetActionModel(nameof(TestController.Delete), new[] { new ProducesErrorResponseTypeAttribute(expected) });
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        var attribute = GetProperty<ProducesErrorResponseTypeAttribute>(action);
        Assert.Equal(expected, attribute.Type);
    }

    private ApiConventionApplicationModelConvention GetConvention(Type errorType = null)
    {
        errorType = errorType ?? typeof(ProblemDetails);
        return new ApiConventionApplicationModelConvention(new ProducesErrorResponseTypeAttribute(errorType));
    }

    private static TValue GetProperty<TValue>(ActionModel action)
    {
        return Assert.IsType<TValue>(action.Properties[typeof(TValue)]);
    }

    private static ActionModel GetActionModel(
        string actionName,
        object[] actionAttributes = null,
        object[] controllerAttributes = null)
    {
        actionAttributes = actionAttributes ?? Array.Empty<object>();
        controllerAttributes = controllerAttributes ?? new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

        var controllerModel = new ControllerModel(typeof(TestController).GetTypeInfo(), controllerAttributes);
        var actionModel = new ActionModel(typeof(TestController).GetMethod(actionName), actionAttributes)
        {
            Controller = controllerModel,
        };

        controllerModel.Actions.Add(actionModel);

        return actionModel;
    }

    private class TestController
    {
        public IActionResult NoMatch() => null;

        public IActionResult Delete(int id) => null;
    }
}
