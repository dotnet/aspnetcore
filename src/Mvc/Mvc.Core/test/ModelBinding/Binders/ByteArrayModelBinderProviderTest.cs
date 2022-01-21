// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class ByteArrayModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(TestClass))]
    [InlineData(typeof(IList<byte>))]
    [InlineData(typeof(int[]))]
    public void Create_ForNonByteArrayTypes_ReturnsNull(Type modelType)
    {
        // Arrange
        var provider = new ByteArrayModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_ForByteArray_ReturnsBinder()
    {
        // Arrange
        var provider = new ByteArrayModelBinderProvider();
        var context = new TestModelBinderProviderContext(typeof(byte[]));

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<ByteArrayModelBinder>(result);
    }

    private class TestClass
    {
    }
}
