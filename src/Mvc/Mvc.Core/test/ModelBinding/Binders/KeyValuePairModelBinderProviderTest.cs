// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class KeyValuePairModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(Person))]
    [InlineData(typeof(KeyValuePair<string, int>?))]
    [InlineData(typeof(KeyValuePair<string, int>[]))]
    public void Create_ForNonKeyValuePair_ReturnsNull(Type modelType)
    {
        // Arrange
        var provider = new KeyValuePairModelBinderProvider();

        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_ForKeyValuePair_ReturnsBinder()
    {
        // Arrange
        var provider = new KeyValuePairModelBinderProvider();

        var context = new TestModelBinderProviderContext(typeof(KeyValuePair<string, int>));
        context.OnCreatingBinder(m =>
        {
            if (m.ModelType == typeof(string) || m.ModelType == typeof(int))
            {
                return Mock.Of<IModelBinder>();
            }
            else
            {
                Assert.False(true, "Not the right model type");
                return null;
            }
        });

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<KeyValuePairModelBinder<string, int>>(result);
    }

    private class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}
