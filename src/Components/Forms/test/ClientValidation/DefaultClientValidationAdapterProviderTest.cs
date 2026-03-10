// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using Moq;

namespace Microsoft.AspNetCore.Components.Forms;

public class DefaultClientValidationAdapterProviderTest
{
    [Theory]
    [InlineData(typeof(RequiredAttribute), typeof(RequiredClientAdapter))]
    [InlineData(typeof(StringLengthAttribute), typeof(StringLengthClientAdapter))]
    [InlineData(typeof(MinLengthAttribute), typeof(MinLengthClientAdapter))]
    [InlineData(typeof(MaxLengthAttribute), typeof(MaxLengthClientAdapter))]
    [InlineData(typeof(RangeAttribute), typeof(RangeClientAdapter))]
    [InlineData(typeof(RegularExpressionAttribute), typeof(RegexClientAdapter))]
    [InlineData(typeof(CompareAttribute), typeof(CompareClientAdapter))]
    public void GetAdapter_ReturnsCorrectAdapterForBuiltInAttribute(Type attributeType, Type expectedAdapterType)
    {
        var provider = new DefaultClientValidationAdapterProvider(
            Enumerable.Empty<IClientValidationAdapterProvider>());

        var attribute = CreateAttribute(attributeType);
        var adapter = provider.GetAdapter(attribute);

        Assert.NotNull(adapter);
        Assert.IsType(expectedAdapterType, adapter);
    }

    [Theory]
    [InlineData(typeof(EmailAddressAttribute))]
    [InlineData(typeof(UrlAttribute))]
    [InlineData(typeof(CreditCardAttribute))]
    [InlineData(typeof(PhoneAttribute))]
    public void GetAdapter_ReturnsDataTypeAdapterForDataTypeAttributes(Type attributeType)
    {
        var provider = new DefaultClientValidationAdapterProvider(
            Enumerable.Empty<IClientValidationAdapterProvider>());

        var attribute = CreateAttribute(attributeType);
        var adapter = provider.GetAdapter(attribute);

        Assert.NotNull(adapter);
        Assert.IsType<DataTypeClientAdapter>(adapter);
    }

    [Fact]
    public void GetAdapter_ReturnsNullForUnknownAttribute()
    {
        var provider = new DefaultClientValidationAdapterProvider(
            Enumerable.Empty<IClientValidationAdapterProvider>());

        var attribute = new CustomValidationAttribute(typeof(object), "Validate");
        var adapter = provider.GetAdapter(attribute);

        Assert.Null(adapter);
    }

    [Fact]
    public void GetAdapter_FallsBackToCustomProviders()
    {
        var mockAdapter = new Mock<IClientValidationAdapter>();
        var customProvider = new Mock<IClientValidationAdapterProvider>();
        customProvider
            .Setup(p => p.GetAdapter(It.IsAny<ValidationAttribute>()))
            .Returns(mockAdapter.Object);

        var provider = new DefaultClientValidationAdapterProvider(
            new[] { customProvider.Object });

        var attribute = new CustomValidationAttribute(typeof(object), "Validate");
        var adapter = provider.GetAdapter(attribute);

        Assert.Same(mockAdapter.Object, adapter);
    }

    [Fact]
    public void GetAdapter_BuiltInTakesPrecedenceOverCustom()
    {
        var mockAdapter = new Mock<IClientValidationAdapter>();
        var customProvider = new Mock<IClientValidationAdapterProvider>();
        customProvider
            .Setup(p => p.GetAdapter(It.IsAny<ValidationAttribute>()))
            .Returns(mockAdapter.Object);

        var provider = new DefaultClientValidationAdapterProvider(
            new[] { customProvider.Object });

        var attribute = new RequiredAttribute();
        var adapter = provider.GetAdapter(attribute);

        Assert.IsType<RequiredClientAdapter>(adapter);
        customProvider.Verify(p => p.GetAdapter(It.IsAny<ValidationAttribute>()), Times.Never);
    }

    [Fact]
    public void GetAdapter_TriesMultipleCustomProviders()
    {
        var mockAdapter = new Mock<IClientValidationAdapter>();

        var provider1 = new Mock<IClientValidationAdapterProvider>();
        provider1
            .Setup(p => p.GetAdapter(It.IsAny<ValidationAttribute>()))
            .Returns((IClientValidationAdapter?)null);

        var provider2 = new Mock<IClientValidationAdapterProvider>();
        provider2
            .Setup(p => p.GetAdapter(It.IsAny<ValidationAttribute>()))
            .Returns(mockAdapter.Object);

        var provider = new DefaultClientValidationAdapterProvider(
            new[] { provider1.Object, provider2.Object });

        var attribute = new CustomValidationAttribute(typeof(object), "Validate");
        var adapter = provider.GetAdapter(attribute);

        Assert.Same(mockAdapter.Object, adapter);
        provider1.Verify(p => p.GetAdapter(attribute), Times.Once);
        provider2.Verify(p => p.GetAdapter(attribute), Times.Once);
    }

    private static ValidationAttribute CreateAttribute(Type attributeType)
    {
        if (attributeType == typeof(StringLengthAttribute))
        {
            return new StringLengthAttribute(100);
        }
        if (attributeType == typeof(MinLengthAttribute))
        {
            return new MinLengthAttribute(1);
        }
        if (attributeType == typeof(MaxLengthAttribute))
        {
            return new MaxLengthAttribute(100);
        }
        if (attributeType == typeof(RangeAttribute))
        {
            return new RangeAttribute(1, 100);
        }
        if (attributeType == typeof(RegularExpressionAttribute))
        {
            return new RegularExpressionAttribute(".*");
        }
        if (attributeType == typeof(CompareAttribute))
        {
            return new CompareAttribute("Other");
        }

        return (ValidationAttribute)Activator.CreateInstance(attributeType)!;
    }
}
