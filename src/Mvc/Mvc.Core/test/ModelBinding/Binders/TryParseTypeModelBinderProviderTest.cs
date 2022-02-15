// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class TryParseModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(object))]
    [InlineData(typeof(Calendar))]
    [InlineData(typeof(TestClass))]
    [InlineData(typeof(List<int>))]
    public void Create_ForTypesWithoutTryParse_ReturnsNull(Type modelType)
    {
        // Arrange
        var provider = new TryParseModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(IPEndPoint))]
    [InlineData(typeof(TestClassWithTryParse))]
    public void Create_ForTypesWithTryParse_ReturnsBinder(Type modelType)
    {
        // Arrange
        var provider = new TryParseModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        var binderType = typeof(TryParseModelBinder<>).MakeGenericType(modelType);
        Assert.IsType(binderType, result);
    }

    private class TestClass
    {
    }

    private class TestClassWithTryParse
    {
        public static bool TryParse(string s, out TestClassWithTryParse result)
        {
            result = new TestClassWithTryParse();
            return true;
        }
    }
}
