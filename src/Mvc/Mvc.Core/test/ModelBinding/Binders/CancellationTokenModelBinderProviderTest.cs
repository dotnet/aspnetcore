// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class CancellationTokenModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(TestClass))]
    [InlineData(typeof(IList<int>))]
    [InlineData(typeof(int[]))]
    public void Create_ForNonCancellationTokenTypes_ReturnsNull(Type modelType)
    {
        // Arrange
        var provider = new CancellationTokenModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_ForCancellationToken_ReturnsBinder()
    {
        // Arrange
        var provider = new CancellationTokenModelBinderProvider();
        var context = new TestModelBinderProviderContext(typeof(CancellationToken));

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<CancellationTokenModelBinder>(result);
    }

    private class TestClass
    {
    }
}
