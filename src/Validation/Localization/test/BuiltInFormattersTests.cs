// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization.Tests;

public class BuiltInFormattersTests
{
    private static readonly ValidationAttributeFormatterRegistry Registry = new();

    [Fact]
    public void Range_FormatsMinAndMax()
    {
        var formatter = Registry.GetFormatter(new RangeAttribute(18, 120));

        var result = formatter!.FormatMessage(
            CultureInfo.InvariantCulture,
            messageTemplate: "{0} must be between {1} and {2}.",
            displayName: "Age");

        Assert.Equal("Age must be between 18 and 120.", result);
    }

    [Fact]
    public void MinLength_FormatsLength()
    {
        var formatter = Registry.GetFormatter(new MinLengthAttribute(3));

        var result = formatter!.FormatMessage(
            CultureInfo.InvariantCulture,
            "{0} must be at least {1} characters.",
            "Name");

        Assert.Equal("Name must be at least 3 characters.", result);
    }

    [Fact]
    public void MaxLength_FormatsLength()
    {
        var formatter = Registry.GetFormatter(new MaxLengthAttribute(50));

        var result = formatter!.FormatMessage(
            CultureInfo.InvariantCulture,
            "{0} must be at most {1} characters.",
            "Name");

        Assert.Equal("Name must be at most 50 characters.", result);
    }

    [Fact]
    public void Length_FormatsMinAndMax()
    {
        var formatter = Registry.GetFormatter(new LengthAttribute(3, 50));

        var result = formatter!.FormatMessage(
            CultureInfo.InvariantCulture,
            "{0} must be between {1} and {2} characters.",
            "Name");

        Assert.Equal("Name must be between 3 and 50 characters.", result);
    }

    [Fact]
    public void StringLength_FormatsMaxThenMin()
    {
        // Note: StringLength's template uses {1}=max, {2}=min (matches BCL convention).
        var formatter = Registry.GetFormatter(new StringLengthAttribute(50) { MinimumLength = 3 });

        var result = formatter!.FormatMessage(
            CultureInfo.InvariantCulture,
            "{0} must be between {2} and {1} characters.",
            "Name");

        Assert.Equal("Name must be between 3 and 50 characters.", result);
    }

    [Fact]
    public void RegularExpression_FormatsPattern()
    {
        var formatter = Registry.GetFormatter(new RegularExpressionAttribute(@"^[A-Z]+$"));

        var result = formatter!.FormatMessage(
            CultureInfo.InvariantCulture,
            "{0} must match {1}.",
            "Code");

        Assert.Equal(@"Code must match ^[A-Z]+$.", result);
    }

    [Fact]
    public void FileExtensions_FormatsExtensions()
    {
        var attr = new FileExtensionsAttribute { Extensions = "png,jpg" };
        var formatter = Registry.GetFormatter(attr);

        var result = formatter!.FormatMessage(
            CultureInfo.InvariantCulture,
            "{0} must have one of these extensions: {1}.",
            "File");

        Assert.Equal("File must have one of these extensions: png,jpg.", result);
    }

    [Fact]
    public void Compare_NoOtherPropertyDisplayName_FallsBackToOtherProperty()
    {
        var attr = new CompareAttribute("ConfirmPassword");
        var formatter = Registry.GetFormatter(attr);

        var result = formatter!.FormatMessage(
            CultureInfo.InvariantCulture,
            "{0} must match {1}.",
            "Password");

        Assert.Equal("Password must match ConfirmPassword.", result);
    }
}
