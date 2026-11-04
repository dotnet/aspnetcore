// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class ApiVisibilityConventionTest
{
    [Fact]
    public void Apply_SetsApiExplorerVisibility()
    {
        // Arrange
        var action = GetActionModel();
        var convention = new ApiVisibilityConvention();

        // Act
        convention.Apply(action);

        // Assert
        Assert.True(action.ApiExplorer.IsVisible);
    }

    [Fact]
    public void Apply_DoesNotSetApiExplorerVisibility_IfAlreadySpecifiedOnAction()
    {
        // Arrange
        var action = GetActionModel();
        action.ApiExplorer.IsVisible = false;
        var convention = new ApiVisibilityConvention();

        // Act
        convention.Apply(action);

        // Assert
        Assert.False(action.ApiExplorer.IsVisible);
    }

    [Fact]
    public void Apply_DoesNotSetApiExplorerVisibility_IfAlreadySpecifiedOnController()
    {
        // Arrange
        var action = GetActionModel();
        action.Controller.ApiExplorer.IsVisible = false;
        var convention = new ApiVisibilityConvention();

        // Act
        convention.Apply(action);

        // Assert
        Assert.Null(action.ApiExplorer.IsVisible);
    }

    private static ActionModel GetActionModel()
    {
        return new ActionModel(typeof(object).GetMethods()[0], new object[0])
        {
            Controller = new ControllerModel(typeof(object).GetTypeInfo(), new object[0]),
        };
    }
}
