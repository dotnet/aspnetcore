// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class ComplexObjectModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(List<int>))]
    public void Create_ForNonComplexType_ReturnsNull(Type modelType)
    {
        // Arrange
        var provider = new ComplexObjectModelBinderProvider();

        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_ForSupportedTypes_ReturnsBinder()
    {
        // Arrange
        var provider = new ComplexObjectModelBinderProvider();

        var context = new TestModelBinderProviderContext(typeof(Person));
        context.OnCreatingBinder(m =>
        {
            if (m.ModelType == typeof(int) || m.ModelType == typeof(string))
            {
                return Mock.Of<IModelBinder>();
            }
            else
            {
                Assert.Fail("Not the right model type");
                return null;
            }
        });

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<ComplexObjectModelBinder>(result);
    }

    [Fact]
    public void Create_ForSupportedType_ReturnsBinder()
    {
        // Arrange
        var provider = new ComplexObjectModelBinderProvider();

        var context = new TestModelBinderProviderContext(typeof(Person));
        context.OnCreatingBinder(m =>
        {
            if (m.ModelType == typeof(int) || m.ModelType == typeof(string))
            {
                return Mock.Of<IModelBinder>();
            }
            else
            {
                Assert.Fail("Not the right model type");
                return null;
            }
        });

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<ComplexObjectModelBinder>(result);
    }

    private class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}
