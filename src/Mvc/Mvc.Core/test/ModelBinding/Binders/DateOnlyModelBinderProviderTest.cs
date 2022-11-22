// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class DateOnlyModelBinderProviderTest
{
    private readonly DateOnlyModelBinderProvider _provider = new DateOnlyModelBinderProvider();

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(DateTimeOffset))]
    [InlineData(typeof(DateTimeOffset?))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(TimeSpan))]
    public void Create_ForNonDateOnly_ReturnsNull(Type modelType)
    {
        // Arrange
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = _provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_ForDateOnly_ReturnsBinder()
    {
        // Arrange
        var context = new TestModelBinderProviderContext(typeof(DateOnly));

        // Act
        var result = _provider.GetBinder(context);

        // Assert
        Assert.IsType<DateOnlyModelBinder>(result);
    }

    [Fact]
    public void Create_ForNullableDateOnly_ReturnsBinder()
    {
        // Arrange
        var context = new TestModelBinderProviderContext(typeof(DateOnly?));

        // Act
        var result = _provider.GetBinder(context);

        // Assert
        Assert.IsType<DateOnlyModelBinder>(result);
    }
}
