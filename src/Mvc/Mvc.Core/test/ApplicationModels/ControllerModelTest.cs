// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class ControllerModelTest
{
    [Fact]
    public void CopyConstructor_DoesDeepCopyOfOtherModels()
    {
        // Arrange
        var controller = new ControllerModel(typeof(TestController).GetTypeInfo(),
                                             new List<object>());

        var action = new ActionModel(typeof(TestController).GetMethod("Edit"),
                                     new List<object>());
        controller.Actions.Add(action);
        action.Controller = controller;

        controller.ControllerProperties.Add(new PropertyModel(
            controller.ControllerType.AsType().GetProperty("TestProperty"),
            new List<object>() { }));

        var route = new AttributeRouteModel(new HttpGetAttribute("api/Products"));
        controller.Selectors.Add(new SelectorModel() { AttributeRouteModel = route });

        var apiExplorer = controller.ApiExplorer;
        controller.ApiExplorer.GroupName = "group";
        controller.ApiExplorer.IsVisible = true;

        // Act
        var controller2 = new ControllerModel(controller);

        // Assert
        Assert.NotSame(action, controller2.Actions[0]);
        Assert.NotNull(controller2.ControllerProperties);
        Assert.Single(controller2.ControllerProperties);
        Assert.NotNull(controller2.Selectors);
        Assert.Single(controller2.Selectors);
        Assert.NotSame(route, controller2.Selectors[0].AttributeRouteModel);
        Assert.NotSame(apiExplorer, controller2.ApiExplorer);

        Assert.NotSame(controller.Selectors[0].ActionConstraints, controller2.Selectors[0].ActionConstraints);
        Assert.NotSame(controller.Actions, controller2.Actions);
        Assert.NotSame(controller.Attributes, controller2.Attributes);
        Assert.NotSame(controller.Filters, controller2.Filters);
        Assert.NotSame(controller.RouteValues, controller2.RouteValues);

        Assert.NotSame(controller, controller2.Actions[0].Controller);
        Assert.Same(controller2, controller2.Actions[0].Controller);
        Assert.NotSame(controller, controller2.ControllerProperties[0].Controller);
        Assert.Same(controller2, controller2.ControllerProperties[0].Controller);
    }

    [Fact]
    public void CopyConstructor_CopiesAllProperties()
    {
        // Arrange
        var controller = new ControllerModel(
            typeof(TestController).GetTypeInfo(),
            new List<object>()
            {
                    new HttpGetAttribute(),
                    new MyFilterAttribute(),
            });

        var selectorModel = new SelectorModel();
        selectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new string[] { "GET" }));
        controller.Selectors.Add(selectorModel);
        controller.Application = new ApplicationModel();
        controller.ControllerName = "cool";
        controller.Filters.Add(new MyFilterAttribute());
        controller.RouteValues.Add("key", "value");
        controller.Properties.Add(new KeyValuePair<object, object>("test key", "test value"));
        controller.ControllerProperties.Add(
            new PropertyModel(typeof(TestController).GetProperty("TestProperty"), new List<object>()));

        // Act
        var controller2 = new ControllerModel(controller);

        // Assert
        foreach (var property in typeof(ControllerModel).GetProperties())
        {
            if (property.Name.Equals("Actions") ||
                property.Name.Equals("Selectors") ||
                property.Name.Equals("ApiExplorer") ||
                property.Name.Equals("ControllerProperties"))
            {
                // This test excludes other ApplicationModel objects on purpose because we deep copy them.
                continue;
            }

            var value1 = property.GetValue(controller);
            var value2 = property.GetValue(controller2);

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
            else if (property.Name.Equals(nameof(ControllerModel.DisplayName)))
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
        public string TestProperty { get; set; }

        public void Edit()
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
