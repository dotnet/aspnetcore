// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Forms;

public class BuiltInAdapterTests
{
    private static (ClientValidationContext Context, Dictionary<string, string> Attributes) CreateContext()
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return (new ClientValidationContext(attributes), attributes);
    }

    [Fact]
    public void RequiredAdapter_EmitsDataValRequired()
    {
        var adapter = new RequiredClientAdapter();
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Name is required.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Name is required.", attributes["data-val-required"]);
        Assert.Equal(2, attributes.Count);
    }

    [Fact]
    public void StringLengthAdapter_EmitsLengthAttributes()
    {
        var attribute = new StringLengthAttribute(100) { MinimumLength = 2 };
        var adapter = new StringLengthClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Must be 2-100 chars.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Must be 2-100 chars.", attributes["data-val-length"]);
        Assert.Equal("100", attributes["data-val-length-max"]);
        Assert.Equal("2", attributes["data-val-length-min"]);
        Assert.Equal(4, attributes.Count);
    }

    [Fact]
    public void StringLengthAdapter_OmitsMinWhenZero()
    {
        var attribute = new StringLengthAttribute(50);
        var adapter = new StringLengthClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Max 50 chars.");

        Assert.Equal("50", attributes["data-val-length-max"]);
        Assert.False(attributes.ContainsKey("data-val-length-min"));
        Assert.Equal(3, attributes.Count);
    }

    [Fact]
    public void StringLengthAdapter_OmitsMaxWhenMaxValue()
    {
        var attribute = new StringLengthAttribute(int.MaxValue) { MinimumLength = 5 };
        var adapter = new StringLengthClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Min 5 chars.");

        Assert.Equal("5", attributes["data-val-length-min"]);
        Assert.False(attributes.ContainsKey("data-val-length-max"));
        Assert.Equal(3, attributes.Count);
    }

    [Fact]
    public void MinLengthAdapter_EmitsMinLengthAttributes()
    {
        var attribute = new MinLengthAttribute(3);
        var adapter = new MinLengthClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "At least 3.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("At least 3.", attributes["data-val-minlength"]);
        Assert.Equal("3", attributes["data-val-minlength-min"]);
        Assert.Equal(3, attributes.Count);
    }

    [Fact]
    public void MaxLengthAdapter_EmitsMaxLengthAttributes()
    {
        var attribute = new MaxLengthAttribute(200);
        var adapter = new MaxLengthClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Max 200.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Max 200.", attributes["data-val-maxlength"]);
        Assert.Equal("200", attributes["data-val-maxlength-max"]);
        Assert.Equal(3, attributes.Count);
    }

    [Fact]
    public void RangeAdapter_EmitsRangeAttributes()
    {
        var attribute = new RangeAttribute(1, 100);
        var adapter = new RangeClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Must be 1-100.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Must be 1-100.", attributes["data-val-range"]);
        Assert.Equal("1", attributes["data-val-range-min"]);
        Assert.Equal("100", attributes["data-val-range-max"]);
        Assert.Equal(4, attributes.Count);
    }

    [Fact]
    public void RangeAdapter_EmitsDoubleValues()
    {
        var attribute = new RangeAttribute(0.5, 99.9);
        var adapter = new RangeClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Must be 0.5-99.9.");

        Assert.Equal("0.5", attributes["data-val-range-min"]);
        Assert.Equal("99.9", attributes["data-val-range-max"]);
    }

    [Fact]
    public void RegexAdapter_EmitsRegexAttributes()
    {
        var attribute = new RegularExpressionAttribute(@"^\d{3}-\d{4}$");
        var adapter = new RegexClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Invalid format.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Invalid format.", attributes["data-val-regex"]);
        Assert.Equal(@"^\d{3}-\d{4}$", attributes["data-val-regex-pattern"]);
        Assert.Equal(3, attributes.Count);
    }

    [Fact]
    public void EmailAdapter_EmitsEmailAttribute()
    {
        var attribute = new EmailAddressAttribute();
        var adapter = new DataTypeClientAdapter("data-val-email");
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Invalid email.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Invalid email.", attributes["data-val-email"]);
        Assert.Equal(2, attributes.Count);
    }

    [Fact]
    public void UrlAdapter_EmitsUrlAttribute()
    {
        var attribute = new UrlAttribute();
        var adapter = new DataTypeClientAdapter("data-val-url");
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Invalid URL.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Invalid URL.", attributes["data-val-url"]);
        Assert.Equal(2, attributes.Count);
    }

    [Fact]
    public void CreditCardAdapter_EmitsCreditCardAttribute()
    {
        var attribute = new CreditCardAttribute();
        var adapter = new DataTypeClientAdapter("data-val-creditcard");
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Invalid card.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Invalid card.", attributes["data-val-creditcard"]);
        Assert.Equal(2, attributes.Count);
    }

    [Fact]
    public void PhoneAdapter_EmitsPhoneAttribute()
    {
        var attribute = new PhoneAttribute();
        var adapter = new DataTypeClientAdapter("data-val-phone");
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Invalid phone.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Invalid phone.", attributes["data-val-phone"]);
        Assert.Equal(2, attributes.Count);
    }

    [Fact]
    public void CompareAdapter_EmitsEqualtoAttributes()
    {
        var attribute = new CompareAttribute("ConfirmPassword");
        var adapter = new CompareClientAdapter(attribute);
        var (context, attributes) = CreateContext();

        adapter.AddClientValidation(in context, "Passwords must match.");

        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Passwords must match.", attributes["data-val-equalto"]);
        Assert.Equal("*.ConfirmPassword", attributes["data-val-equalto-other"]);
        Assert.Equal(3, attributes.Count);
    }

    [Fact]
    public void Adapter_DoesNotOverwriteExistingDataVal()
    {
        var (context, attributes) = CreateContext();

        var requiredAdapter = new RequiredClientAdapter();
        requiredAdapter.AddClientValidation(in context, "Required.");

        var lengthAdapter = new StringLengthClientAdapter(new StringLengthAttribute(50));
        lengthAdapter.AddClientValidation(in context, "Too long.");

        // data-val="true" was set by required, length should not overwrite
        Assert.Equal("true", attributes["data-val"]);
        // Both adapters' unique keys are present
        Assert.True(attributes.ContainsKey("data-val-required"));
        Assert.True(attributes.ContainsKey("data-val-length"));
    }
}
