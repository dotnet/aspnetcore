// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class EnumTypeModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(CarType))]
    [InlineData(typeof(CarType?))]
    public void ReturnsBinder_ForEnumType(Type modelType)
    {
        // Arrange
        var provider = new EnumTypeModelBinderProvider(new MvcOptions());
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<EnumTypeModelBinder>(result);
    }

    [Theory]
    [InlineData(typeof(CarOptions))]
    [InlineData(typeof(CarOptions?))]
    public void ReturnsBinder_ForFlagsEnumType(Type modelType)
    {
        // Arrange
        var provider = new EnumTypeModelBinderProvider(new MvcOptions());
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<EnumTypeModelBinder>(result);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(int?))]
    public void DoesNotReturnBinder_ForNonEnumTypes(Type modelType)
    {
        // Arrange
        var provider = new EnumTypeModelBinderProvider(new MvcOptions());
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    enum CarType
    {
        Sedan,
        Coupe
    }

    [Flags]
    public enum CarOptions
    {
        SunRoof = 0x01,
        Spoiler = 0x02,
        FogLights = 0x04,
        TintedWindows = 0x08,
    }
}
