// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class InvalidModelStateFilterConventionTest
{
    [Fact]
    public void Apply_AddsFilter()
    {
        // Arrange
        var action = GetActionModel();
        var convention = GetConvention();

        // Act
        convention.Apply(action);

        // Assert
        Assert.Single(action.Filters.OfType<ModelStateInvalidFilterFactory>());
    }

    private static ActionModel GetActionModel()
    {
        var action = new ActionModel(typeof(object).GetMethods()[0], new object[0]);

        return action;
    }

    private InvalidModelStateFilterConvention GetConvention()
    {
        return new InvalidModelStateFilterConvention();
    }
}
