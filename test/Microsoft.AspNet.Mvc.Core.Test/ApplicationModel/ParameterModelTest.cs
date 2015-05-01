// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Xunit;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class ParameterModelTest
    {
        [Fact]
        public void CopyConstructor_CopiesAllProperties()
        {
            // Arrange
            var parameter = new ParameterModel(typeof(TestController).GetMethod("Edit").GetParameters()[0],
                                               new List<object>() { new FromBodyAttribute() });

            parameter.Action = new ActionModel(typeof(TestController).GetMethod("Edit"), new List<object>());
            parameter.BindingInfo = new BindingInfo()
            {
                BindingSource = BindingSource.Body
            };

            parameter.ParameterName = "id";

            // Act
            var parameter2 = new ParameterModel(parameter);

            // Assert
            foreach (var property in typeof(ParameterModel).GetProperties())
            {
                var value1 = property.GetValue(parameter);
                var value2 = property.GetValue(parameter2);

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