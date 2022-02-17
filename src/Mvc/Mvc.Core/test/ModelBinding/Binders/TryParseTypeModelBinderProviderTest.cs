// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class TryParseModelBinderProviderTest
{
    [Fact]
    public void Create_ForTypesWithoutTryParse_ReturnsNull()
    {
        // Arrange
        var provider = new TryParseModelBinderProvider();
        var context = CreateContext(modelType: typeof(TestClass), false);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_ForTypesWithTryParse_ReturnsBinder()
    {
        // Arrange
        var provider = new TryParseModelBinderProvider();
        var context = CreateContext(typeof(TestClassWithTryParse), true);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<TryParseModelBinder>(result);
    }

    private static TestModelBinderProviderContext CreateContext(Type modelType, bool allowTryParse)
    {
        TestModelBinderProviderContext.CachedMetadataProvider
            .ForType(modelType)
            .BindingDetails(b => b.IsBindingFromTryParseAllowed = allowTryParse);

        return new TestModelBinderProviderContext(modelType);
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
