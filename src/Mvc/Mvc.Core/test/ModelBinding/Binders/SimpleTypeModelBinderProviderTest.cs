// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class SimpleTypeModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(Calendar))]
    [InlineData(typeof(TestClass))]
    [InlineData(typeof(List<int>))]
    public void Create_ForCollectionOrComplexTypes_ReturnsNull(Type modelType)
    {
        // Arrange
        var provider = new SimpleTypeModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(string))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateTime?))]
    public void Create_ForSimpleTypes_ReturnsBinder(Type modelType)
    {
        // Arrange
        var provider = new SimpleTypeModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<SimpleTypeModelBinder>(result);
    }

    private class TestClass
    {
    }
}
