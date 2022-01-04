// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class ActionModelTest
{
    [Fact]
    public void CopyConstructor_DoesDeepCopyOfOtherModels()
    {
        // Arrange
        var action = new ActionModel(typeof(TestController).GetMethod(nameof(TestController.Edit)),
                                     new List<object>());

        var parameter = new ParameterModel(action.ActionMethod.GetParameters()[0],
                                           new List<object>());
        parameter.Action = action;
        action.Parameters.Add(parameter);

        var route = new AttributeRouteModel(new HttpGetAttribute("api/Products"));
        action.Selectors.Add(new SelectorModel()
        {
            AttributeRouteModel = route
        });

        var apiExplorer = action.ApiExplorer;
        apiExplorer.IsVisible = false;
        apiExplorer.GroupName = "group1";

        // Act
        var action2 = new ActionModel(action);

        // Assert
        Assert.NotSame(action.Parameters, action2.Parameters);
        Assert.NotNull(action2.Parameters);
        Assert.Single(action2.Parameters);
        Assert.NotSame(parameter, action2.Parameters[0]);
        Assert.NotSame(apiExplorer, action2.ApiExplorer);
        Assert.NotSame(action.Selectors, action2.Selectors);
        Assert.NotNull(action2.Selectors);
        Assert.Single(action2.Selectors);
        Assert.NotSame(action.Selectors[0], action2.Selectors[0]);
        Assert.NotSame(route, action2.Selectors[0].AttributeRouteModel);

        Assert.NotSame(action, action2.Parameters[0].Action);
        Assert.Same(action2, action2.Parameters[0].Action);
    }

    [Fact]
    public void CopyConstructor_CopiesAllProperties()
    {
        // Arrange
        var action = new ActionModel(
            typeof(TestController).GetMethod("Edit"),
            new List<object>()
            {
                    new HttpGetAttribute(),
                    new MyFilterAttribute(),
            });

        var selectorModel = new SelectorModel();
        selectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new string[] { "GET" }));
        action.Selectors.Add(selectorModel);
        action.ActionName = "Edit";

        action.Controller = new ControllerModel
            (typeof(TestController).GetTypeInfo(),
            new List<object>());
        action.Filters.Add(new MyFilterAttribute());
        action.RouteParameterTransformer = Mock.Of<IOutboundParameterTransformer>();
        action.RouteValues.Add("key", "value");
        action.Properties.Add(new KeyValuePair<object, object>("test key", "test value"));

        // Act
        var action2 = new ActionModel(action);

        // Assert
        foreach (var property in typeof(ActionModel).GetProperties())
        {
            // Reflection is used to make sure the test fails when a new property is added.
            if (property.Name.Equals("ApiExplorer") ||
                property.Name.Equals("Selectors") ||
                property.Name.Equals("Parameters"))
            {
                // This test excludes other ApplicationModel objects on purpose because we deep copy them.
                continue;
            }

            var value1 = property.GetValue(action);
            var value2 = property.GetValue(action2);

            if (typeof(IEnumerable<object>).IsAssignableFrom(property.PropertyType))
            {
                Assert.Equal<object>((IEnumerable<object>)value1, (IEnumerable<object>)value2);

                // Ensure non-default value
                Assert.NotEmpty((IEnumerable<object>)value1);
            }
            else if (typeof(IDictionary<string, string>).IsAssignableFrom(property.PropertyType))
            {
                Assert.Equal(value1, value2);

                // Ensure non-default value
                Assert.NotEmpty((IDictionary<string, string>)value1);
            }
            else if (typeof(IDictionary<object, object>).IsAssignableFrom(property.PropertyType))
            {
                Assert.Equal(value1, value2);

                // Ensure non-default value
                Assert.NotEmpty((IDictionary<object, object>)value1);
            }
            else if (property.PropertyType.IsValueType ||
                Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                Assert.Equal(value1, value2);

                // Ensure non-default value
                Assert.NotEqual(value1, Activator.CreateInstance(property.PropertyType));
            }
            else if (property.Name.Equals(nameof(ActionModel.DisplayName)))
            {
                // DisplayName is re-calculated, hence reference equality wouldn't work.
                Assert.Equal(value1, value2);
            }
            else
            {
                Assert.Same(value1, value2);

                // Ensure non-default value
                Assert.NotNull(value1);
            }
        }
    }

    private class TestController
    {
        public void Edit(int id)
        {
        }
    }

    private class MyFilterAttribute : Attribute, IFilterMetadata
    {
    }

    private class MyRouteValueAttribute : Attribute, IRouteValueProvider
    {
        public string RouteKey { get; set; }

        public string RouteValue { get; set; }
    }
}
