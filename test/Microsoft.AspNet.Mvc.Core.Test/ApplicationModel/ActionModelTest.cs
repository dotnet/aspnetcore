// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ActionConstraints;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class ActionModelTest
    {
        [Fact]
        public void CopyConstructor_DoesDeepCopyOfOtherModels()
        {
            // Arrange
            var action = new ActionModel(typeof(TestController).GetMethod("Edit"),
                                         new List<object>());

            var parameter = new ParameterModel(action.ActionMethod.GetParameters()[0],
                                               new List<object>());
            parameter.Action = action;
            action.Parameters.Add(parameter);

            var route = new AttributeRouteModel(new HttpGetAttribute("api/Products"));
            action.AttributeRouteModel = route;

            var apiExplorer = action.ApiExplorer;
            apiExplorer.IsVisible = false;
            apiExplorer.GroupName = "group1";

            // Act
            var action2 = new ActionModel(action);

            // Assert
            Assert.NotSame(action, action2.Parameters[0]);
            Assert.NotSame(apiExplorer, action2.ApiExplorer);
            Assert.NotSame(route, action2.AttributeRouteModel);
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

            action.ActionConstraints.Add(new HttpMethodConstraint(new string[] { "GET" }));
            action.ActionName = "Edit";

            action.Controller = new ControllerModel(typeof(TestController).GetTypeInfo(),
                                                    new List<object>());
            action.Filters.Add(new MyFilterAttribute());
            action.HttpMethods.Add("GET");
            action.RouteConstraints.Add(new MyRouteConstraintAttribute());
            action.Properties.Add(new KeyValuePair<object, object>("test key", "test value"));

            // Act
            var action2 = new ActionModel(action);

            // Assert
            foreach (var property in typeof(ActionModel).GetProperties())
            {
                // Reflection is used to make sure the test fails when a new property is added.
                if (property.Name.Equals("ApiExplorer") ||
                    property.Name.Equals("AttributeRouteModel") ||
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
                else if (typeof(IDictionary<object, object>).IsAssignableFrom(property.PropertyType))
                {
                    Assert.Equal(value1, value2);

                    // Ensure non-default value
                    Assert.NotEmpty((IDictionary<object, object>)value1);
                }
                else if (property.PropertyType.GetTypeInfo().IsValueType ||
                    Nullable.GetUnderlyingType(property.PropertyType) != null)
                {
                    Assert.Equal(value1, value2);

                    // Ensure non-default value
                    Assert.NotEqual(value1, Activator.CreateInstance(property.PropertyType));
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

        private class MyRouteConstraintAttribute : Attribute, IRouteConstraintProvider
        {
            public bool BlockNonAttributedActions { get { return true; } }

            public string RouteKey { get; set; }

            public RouteKeyHandling RouteKeyHandling { get { return RouteKeyHandling.RequireKey; } }

            public string RouteValue { get; set; }
        }
    }
}