// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation.LocalizationTests;

public class DefaultAttributeArgumentProviderTests
{
    private readonly DefaultAttributeArgumentProvider _provider = new();

    [Fact]
    public void GetFormatArgs_RequiredAttribute_ReturnsDisplayNameOnly()
    {
        var attribute = new RequiredAttribute();
        var result = _provider.GetFormatArgs(attribute, "UserName");

        Assert.Single(result);
        Assert.Equal("UserName", result[0]);
    }

    [Fact]
    public void GetFormatArgs_RangeAttribute_ReturnsDisplayNameMinMax()
    {
        var attribute = new RangeAttribute(1, 100);
        var result = _provider.GetFormatArgs(attribute, "Age");

        Assert.Equal(3, result.Length);
        Assert.Equal("Age", result[0]);
        Assert.Equal(1, result[1]);
        Assert.Equal(100, result[2]);
    }

    [Fact]
    public void GetFormatArgs_StringLengthAttribute_ReturnsDisplayNameMaxMin()
    {
        var attribute = new StringLengthAttribute(50) { MinimumLength = 5 };
        var result = _provider.GetFormatArgs(attribute, "Name");

        Assert.Equal(3, result.Length);
        Assert.Equal("Name", result[0]);
        Assert.Equal(50, result[1]);
        Assert.Equal(5, result[2]);
    }

    [Fact]
    public void GetFormatArgs_MinLengthAttribute_ReturnsDisplayNameLength()
    {
        var attribute = new MinLengthAttribute(3);
        var result = _provider.GetFormatArgs(attribute, "Code");

        Assert.Equal(2, result.Length);
        Assert.Equal("Code", result[0]);
        Assert.Equal(3, result[1]);
    }

    [Fact]
    public void GetFormatArgs_MaxLengthAttribute_ReturnsDisplayNameLength()
    {
        var attribute = new MaxLengthAttribute(255);
        var result = _provider.GetFormatArgs(attribute, "Description");

        Assert.Equal(2, result.Length);
        Assert.Equal("Description", result[0]);
        Assert.Equal(255, result[1]);
    }

    [Fact]
    public void GetFormatArgs_RegularExpressionAttribute_ReturnsDisplayNamePattern()
    {
        var attribute = new RegularExpressionAttribute(@"^\d+$");
        var result = _provider.GetFormatArgs(attribute, "ZipCode");

        Assert.Equal(2, result.Length);
        Assert.Equal("ZipCode", result[0]);
        Assert.Equal(@"^\d+$", result[1]);
    }

    [Fact]
    public void GetFormatArgs_CompareAttribute_ReturnsDisplayNameOtherProperty()
    {
        var attribute = new CompareAttribute("Password");
        var result = _provider.GetFormatArgs(attribute, "ConfirmPassword");

        Assert.Equal(2, result.Length);
        Assert.Equal("ConfirmPassword", result[0]);
        Assert.Equal("Password", result[1]);
    }

    [Fact]
    public void GetFormatArgs_FileExtensionsAttribute_ReturnsDisplayNameExtensions()
    {
        var attribute = new FileExtensionsAttribute { Extensions = "jpg,png,gif" };
        var result = _provider.GetFormatArgs(attribute, "Photo");

        Assert.Equal(2, result.Length);
        Assert.Equal("Photo", result[0]);
        Assert.Equal("jpg,png,gif", result[1]);
    }

    [Fact]
    public void GetFormatArgs_LengthAttribute_ReturnsDisplayNameMinMax()
    {
        var attribute = new LengthAttribute(2, 10);
        var result = _provider.GetFormatArgs(attribute, "Items");

        Assert.Equal(3, result.Length);
        Assert.Equal("Items", result[0]);
        Assert.Equal(2, result[1]);
        Assert.Equal(10, result[2]);
    }

    [Fact]
    public void GetFormatArgs_UnknownAttribute_ReturnsDisplayNameOnly()
    {
        var attribute = new CustomTestAttribute();
        var result = _provider.GetFormatArgs(attribute, "Field");

        Assert.Single(result);
        Assert.Equal("Field", result[0]);
    }

    [Fact]
    public void GetFormatArgs_EmailAddressAttribute_ReturnsDisplayNameOnly()
    {
        var attribute = new EmailAddressAttribute();
        var result = _provider.GetFormatArgs(attribute, "Email");

        Assert.Single(result);
        Assert.Equal("Email", result[0]);
    }

    private class CustomTestAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => ValidationResult.Success;
    }
}
