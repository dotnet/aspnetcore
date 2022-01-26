// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

#nullable enable
public class NullableEmptyBodyBindingMetadataProviderTest
{
    [Fact]
    public void IsEmptyBodyAllowed_LeftAlone_WhenAlreadySet()
    {
        // Arrange
        var provider = new NullableEmptyBodyBindingMetadataProvider();

        var context = new BindingMetadataProviderContext(
          ModelMetadataIdentity.ForParameter(ParameterInfos.NullableParameter),
          new ModelAttributes(Array.Empty<object>()));

        context.BindingMetadata.IsEmptyBodyAllowed = false;

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsEmptyBodyAllowed);
    }

    [Fact]
    public void IsEmptyBodyAllowed_LeftAlone_WhenNotOptional()
    {
        // Arrange
        var provider = new NullableEmptyBodyBindingMetadataProvider();

        var context = new BindingMetadataProviderContext(
          ModelMetadataIdentity.ForParameter(ParameterInfos.NonNullableParameter),
          new ModelAttributes(Array.Empty<object>()));

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Null(context.BindingMetadata.IsEmptyBodyAllowed);
    }


    [Fact]
    public void IsEmptyBodyAllowed_IsTrue_WhenDefaultValue()
    {
        // Arrange
        var provider = new NullableEmptyBodyBindingMetadataProvider();

        var context = new BindingMetadataProviderContext(
          ModelMetadataIdentity.ForParameter(ParameterInfos.ParameterWithDefaultValue),
          new ModelAttributes(Array.Empty<object>()));

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsEmptyBodyAllowed);

    }

    [Fact]
    public void IsEmptyBodyAllowed_IsTrue_WhenNullable()
    {
        // Arrange
        var provider = new NullableEmptyBodyBindingMetadataProvider();

        var context = new BindingMetadataProviderContext(
          ModelMetadataIdentity.ForParameter(ParameterInfos.NullableParameter),
          new ModelAttributes(Array.Empty<object>()));

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsEmptyBodyAllowed);

    }

    private class ParameterInfos
    {
        public void Method(object param1, object? param2)
        {
        }

#nullable disable
        public void Method2(object param1 = null)
        {
        }
#nullable restore

        public static ParameterInfo NonNullableParameter
            = typeof(ParameterInfos)!
                .GetMethod(nameof(ParameterInfos.Method))!
                .GetParameters()[0];

        public static ParameterInfo NullableParameter
             = typeof(ParameterInfos)!
                .GetMethod(nameof(ParameterInfos.Method))!
                .GetParameters()[1];

        public static ParameterInfo ParameterWithDefaultValue
            = typeof(ParameterInfos)!
                .GetMethod(nameof(ParameterInfos.Method2))!
                .GetParameters()[0];
    }
}
