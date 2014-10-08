// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.ApplicationModel
{
    public class ActionModelTest
    {
        [Fact]
        public void CopyConstructor_DoesDeepCopyOfOtherModels()
        {
            // Arrange
            var action = new ActionModel(typeof(TestController).GetMethod("Edit"));

            var parameter = new ParameterModel(action.ActionMethod.GetParameters()[0]);
            parameter.Action = action;
            action.Parameters.Add(parameter);

            var route = new AttributeRouteModel(new HttpGetAttribute("api/Products"));
            action.AttributeRouteModel = route;

            // Act
            var action2 = new ActionModel(action);

            // Assert
            Assert.NotSame(action, action2.Parameters[0]);
            Assert.NotSame(route, action2.AttributeRouteModel);
        }

        [Fact]
        public void CopyConstructor_CopiesAllProperties()
        {
            // Arrange
            var action = new ActionModel(typeof(TestController).GetMethod("Edit"));

            action.ActionConstraints.Add(new HttpMethodConstraint(new string[] { "GET" }));
            action.ActionName = "Edit";
            action.ApiExplorerGroupName = "group";
            action.ApiExplorerIsVisible = true;
            action.Attributes.Add(new HttpGetAttribute());
            action.Controller = new ControllerModel(typeof(TestController).GetTypeInfo());
            action.Filters.Add(new AuthorizeAttribute());
            action.HttpMethods.Add("GET");
            action.IsActionNameMatchRequired = true;

            // Act
            var action2 = new ActionModel(action);

            // Assert
            foreach (var property in typeof(ActionModel).GetProperties())
            {
                if (property.Name.Equals("Parameters") || property.Name.Equals("AttributeRouteModel"))
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
            public void Edit(int id)
            {
            }
        }
    }
}