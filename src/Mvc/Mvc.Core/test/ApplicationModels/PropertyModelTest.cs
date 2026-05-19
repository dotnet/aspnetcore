// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class PropertyModelTest
{
    [Fact]
    public void CopyConstructor_CopiesAllProperties()
    {
        // Arrange
        var propertyModel = new PropertyModel(typeof(TestController).GetProperty("Property"),
                                           new List<object>() { new FromBodyAttribute() });

        propertyModel.Controller = new ControllerModel(typeof(TestController).GetTypeInfo(), new List<object>());
        propertyModel.BindingInfo = BindingInfo.GetBindingInfo(propertyModel.Attributes);
        propertyModel.PropertyName = "Property";
        propertyModel.Properties.Add(new KeyValuePair<object, object>("test key", "test value"));

        // Act
        var propertyModel2 = new PropertyModel(propertyModel);

        // Assert
        foreach (var property in typeof(PropertyModel).GetProperties())
        {
            if (property.Name.Equals("BindingInfo"))
            {
                // This test excludes other mutable objects on purpose because we deep copy them.
                continue;
            }

            var value1 = property.GetValue(propertyModel);
            var value2 = property.GetValue(propertyModel2);

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
        public string Property { get; set; }
    }
}
