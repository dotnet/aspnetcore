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
        var context = new TestModelBinderProviderContext(typeof(TestClass));

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
        var context = new TestModelBinderProviderContext(typeof(TestClassWithTryParse));

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<TryParseModelBinder>(result);
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
