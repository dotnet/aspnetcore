// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Authorization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
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

            var route = new AttributeRouteModel(new HttpGetAttribute("api/Products"));
            controller.AttributeRoutes.Add(route);

            var apiExplorer = controller.ApiExplorer;
            controller.ApiExplorer.GroupName = "group";
            controller.ApiExplorer.IsVisible = true;

            // Act
            var controller2 = new ControllerModel(controller);

            // Assert
            Assert.NotSame(action, controller2.Actions[0]);
            Assert.NotSame(route, controller2.AttributeRoutes[0]);
            Assert.NotSame(apiExplorer, controller2.ApiExplorer);

            Assert.NotSame(controller.ActionConstraints, controller2.ActionConstraints);
            Assert.NotSame(controller.Actions, controller2.Actions);
            Assert.NotSame(controller.Attributes, controller2.Attributes);
            Assert.NotSame(controller.Filters, controller2.Filters);
            Assert.NotSame(controller.RouteConstraints, controller2.RouteConstraints);
        }

        [Fact]
        public void CopyConstructor_CopiesAllProperties()
        {
            // Arrange
            var controller = new ControllerModel(typeof(TestController).GetTypeInfo(),
                                                 new List<object>() { new HttpGetAttribute(), new AuthorizeAttribute() });

            controller.ActionConstraints.Add(new HttpMethodConstraint(new string[] { "GET" }));
            controller.Application = new ApplicationModel();
            controller.ControllerName = "cool";
            controller.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().Build()));
            controller.RouteConstraints.Add(new AreaAttribute("Admin"));
            controller.Properties.Add(new KeyValuePair<object, object>("test key", "test value"));

            // Act
            var controller2 = new ControllerModel(controller);

            // Assert
            foreach (var property in typeof(ControllerModel).GetProperties())
            {
                if (property.Name.Equals("Actions") ||
                    property.Name.Equals("AttributeRoutes") ||
                    property.Name.Equals("ApiExplorer"))
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
            public void Edit()
            {
            }
        }
    }
}