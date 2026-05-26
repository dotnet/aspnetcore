// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization.Tests;

/// <summary>
/// Tests for <see cref="ValidationAttributeFormatterRegistry"/>: built-in formatters,
/// resolution order, custom registration, and self-formatting attributes.
/// </summary>
public class ValidationAttributeFormatterRegistryTests
{
    [Theory]
    [InlineData(typeof(RangeAttribute))]
    [InlineData(typeof(MinLengthAttribute))]
    [InlineData(typeof(MaxLengthAttribute))]
    [InlineData(typeof(LengthAttribute))]
    [InlineData(typeof(StringLengthAttribute))]
    [InlineData(typeof(RegularExpressionAttribute))]
    [InlineData(typeof(FileExtensionsAttribute))]
    [InlineData(typeof(CompareAttribute))]
    public void GetFormatter_AllBuiltInAttributes_HaveFormatters(Type attributeType)
    {
        // Construct each attribute type with reasonable args.
        ValidationAttribute attribute = attributeType switch
        {
            var t when t == typeof(RangeAttribute) => new RangeAttribute(1, 10),
            var t when t == typeof(MinLengthAttribute) => new MinLengthAttribute(1),
            var t when t == typeof(MaxLengthAttribute) => new MaxLengthAttribute(10),
            var t when t == typeof(LengthAttribute) => new LengthAttribute(1, 10),
            var t when t == typeof(StringLengthAttribute) => new StringLengthAttribute(10),
            var t when t == typeof(RegularExpressionAttribute) => new RegularExpressionAttribute(".*"),
            var t when t == typeof(FileExtensionsAttribute) => new FileExtensionsAttribute(),
            var t when t == typeof(CompareAttribute) => new CompareAttribute("OtherProp"),
            _ => throw new InvalidOperationException(),
        };
        var registry = new ValidationAttributeFormatterRegistry();

        var formatter = registry.GetFormatter(attribute);

        Assert.NotNull(formatter);
    }

    [Fact]
    public void GetFormatter_AttributeWithoutFormatter_ReturnsNull()
    {
        // EmailAddressAttribute has no built-in formatter (its message only uses {0}).
        var registry = new ValidationAttributeFormatterRegistry();

        var formatter = registry.GetFormatter(new EmailAddressAttribute());

        Assert.Null(formatter);
    }

    [Fact]
    public void GetFormatter_SelfFormattingAttribute_ReturnsAttributeItself()
    {
        // When the attribute itself implements IValidationAttributeFormatter, it wins
        // over any registered factory.
        var registry = new ValidationAttributeFormatterRegistry();
        var attribute = new SelfFormattingAttribute();

        var formatter = registry.GetFormatter(attribute);

        Assert.Same(attribute, formatter);
    }

    [Fact]
    public void AddFormatter_RegistersForCustomAttribute_GetReturnsIt()
    {
        var registry = new ValidationAttributeFormatterRegistry();
        registry.AddFormatter<CustomAttribute>(attr => new CustomAttributeFormatter(attr));

        var formatter = registry.GetFormatter(new CustomAttribute());

        Assert.NotNull(formatter);
        Assert.IsType<CustomAttributeFormatter>(formatter);
    }

    [Fact]
    public void AddFormatter_LaterRegistrationReplacesEarlier()
    {
        var registry = new ValidationAttributeFormatterRegistry();
        registry.AddFormatter<CustomAttribute>(_ => new CustomAttributeFormatter(new CustomAttribute()));
        registry.AddFormatter<CustomAttribute>(_ => new ReplacementFormatter());

        var formatter = registry.GetFormatter(new CustomAttribute());

        Assert.IsType<ReplacementFormatter>(formatter);
    }

    [Fact]
    public void AddFormatter_OverridesBuiltIn()
    {
        var registry = new ValidationAttributeFormatterRegistry();
        registry.AddFormatter<RangeAttribute>(_ => new ReplacementFormatter());

        var formatter = registry.GetFormatter(new RangeAttribute(1, 10));

        Assert.IsType<ReplacementFormatter>(formatter);
    }

    [Fact]
    public void AddFormatter_NullFactory_ThrowsArgumentNullException()
    {
        var registry = new ValidationAttributeFormatterRegistry();

        Assert.Throws<ArgumentNullException>(() =>
            registry.AddFormatter<CustomAttribute>(null!));
    }

    [Fact]
    public void AddFormatter_FactoryReceivesAttributeInstance()
    {
        var registry = new ValidationAttributeFormatterRegistry();
        CustomAttribute? receivedAttribute = null;
        registry.AddFormatter<CustomAttribute>(attr =>
        {
            receivedAttribute = attr;
            return new CustomAttributeFormatter(attr);
        });
        var attribute = new CustomAttribute { Extra = "X" };

        registry.GetFormatter(attribute);

        Assert.Same(attribute, receivedAttribute);
    }

    private sealed class CustomAttribute : ValidationAttribute
    {
        public string? Extra { get; set; }
    }

    private sealed class CustomAttributeFormatter(CustomAttribute attribute) : IValidationAttributeFormatter
    {
        public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
            => string.Format(culture, messageTemplate, displayName, attribute.Extra);
    }

    private sealed class ReplacementFormatter : IValidationAttributeFormatter
    {
        public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
            => "REPLACEMENT";
    }

    private sealed class SelfFormattingAttribute : ValidationAttribute, IValidationAttributeFormatter
    {
        public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
            => $"Self: {displayName}";
    }
}
