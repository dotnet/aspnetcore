// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class TimeOnlyModelBinderProviderTest
{
    private readonly TimeOnlyModelBinderProvider _provider = new TimeOnlyModelBinderProvider();

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(DateTimeOffset))]
    [InlineData(typeof(DateTimeOffset?))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(TimeSpan))]
    public void Create_ForNonDateTime_ReturnsNull(Type modelType)
    {
        // Arrange
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = _provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_ForTimeOnly_ReturnsBinder()
    {
        // Arrange
        var context = new TestModelBinderProviderContext(typeof(TimeOnly));

        // Act
        var result = _provider.GetBinder(context);

        // Assert
        Assert.IsType<TimeOnlyModelBinder>(result);
    }

    [Fact]
    public void Create_ForNullableTimeOnly_ReturnsBinder()
    {
        // Arrange
        var context = new TestModelBinderProviderContext(typeof(TimeOnly?));

        // Act
        var result = _provider.GetBinder(context);

        // Assert
        Assert.IsType<TimeOnlyModelBinder>(result);
    }
}
