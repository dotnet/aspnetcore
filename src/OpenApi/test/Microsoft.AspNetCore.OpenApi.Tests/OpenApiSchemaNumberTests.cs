// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Numerics;
using Microsoft.AspNetCore.OpenApi;

#pragma warning disable ASP0040

public class OpenApiSchemaNumberTests
{
    [Theory]
    [InlineData("0", "0", 0, "0")]
    [InlineData("-0", "0", 0, "0")]
    [InlineData("1.2300", "123", -2, "1.23")]
    [InlineData("123e-2", "123", -2, "1.23")]
    [InlineData("1200e-2", "12", 0, "12")]
    [InlineData("-0.00000120", "-12", -7, "-0.0000012")]
    [InlineData("1e1000000", "1", 1000000, "1e+1000000")]
    [InlineData("1e-1000000", "1", -1000000, "1e-1000000")]
    public void Parse_NormalizesAndFormatsExactly(
        string value,
        string expectedSignificand,
        int expectedExponent,
        string expectedText)
    {
        var number = OpenApiSchemaNumber.Parse(value);

        Assert.Equal(BigInteger.Parse(expectedSignificand, CultureInfo.InvariantCulture), number.Significand);
        Assert.Equal(expectedExponent, number.Exponent);
        Assert.Equal(expectedText, number.ToString());
    }

    [Fact]
    public void Parse_PreservesValuesBeyondFixedPrecisionTypes()
    {
        const string Value = "123456789012345678901234567890.12345678901234567890123456789";

        var number = OpenApiSchemaNumber.Parse(Value);

        Assert.Equal(Value, number.ToString());
    }

    [Theory]
    [InlineData("1.2", "1.19", 1)]
    [InlineData("1e1000000", "9e999999", 1)]
    [InlineData("0.0001", "1e-5", 1)]
    [InlineData("-1.2", "-1.19", -1)]
    [InlineData("-1e1000000", "-9e999999", -1)]
    [InlineData("1.2300", "123e-2", 0)]
    [InlineData("-0", "0e100", 0)]
    public void CompareTo_OrdersAcrossSignsAndScales(string left, string right, int expectedSign)
    {
        var leftNumber = OpenApiSchemaNumber.Parse(left);
        var rightNumber = OpenApiSchemaNumber.Parse(right);

        Assert.Equal(expectedSign, Math.Sign(leftNumber.CompareTo(rightNumber)));
        Assert.Equal(expectedSign == 0, leftNumber == rightNumber);
        Assert.Equal(expectedSign < 0, leftNumber < rightNumber);
        Assert.Equal(expectedSign <= 0, leftNumber <= rightNumber);
        Assert.Equal(expectedSign > 0, leftNumber > rightNumber);
        Assert.Equal(expectedSign >= 0, leftNumber >= rightNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("+1")]
    [InlineData("01")]
    [InlineData(".1")]
    [InlineData("1.")]
    [InlineData("1e")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1,5")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("1e2147483648")]
    [InlineData("10e2147483647")]
    [InlineData("1e999999999999999999999999")]
    public void Parse_RejectsInvalidOrUnrepresentableJsonNumbers(string value)
        => Assert.Throws<FormatException>(() => OpenApiSchemaNumber.Parse(value));

    [Fact]
    public void Parse_RejectsPathologicalInputLength()
        => Assert.Throws<FormatException>(() => OpenApiSchemaNumber.Parse(new string('1', 10_001)));

    [Fact]
    public void Constructor_NormalizesAndBoundsSignificand()
    {
        var number = new OpenApiSchemaNumber(new BigInteger(-12300), -2);

        Assert.Equal(new BigInteger(-123), number.Significand);
        Assert.Equal(0, number.Exponent);
        Assert.Equal("-123", number.ToString());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OpenApiSchemaNumber(BigInteger.Parse(new string('1', 10_001), CultureInfo.InvariantCulture), 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OpenApiSchemaNumber(new BigInteger(10), int.MaxValue));
    }

    [Fact]
    public void Conversions_PreserveIntegerAndDecimalValues()
    {
        OpenApiSchemaNumber integer = 42;
        OpenApiSchemaNumber longInteger = long.MaxValue;
        OpenApiSchemaNumber bigInteger = BigInteger.Pow(10, 100);
        OpenApiSchemaNumber decimalNumber = 12.3400m;

        Assert.Equal("42", integer.ToString());
        Assert.Equal(long.MaxValue.ToString(CultureInfo.InvariantCulture), longInteger.ToString());
        Assert.Equal("1e+100", bigInteger.ToString());
        Assert.Equal("12.34", decimalNumber.ToString());
    }

    [Fact]
    public void ScalarEvidence_AcceptsFractionalBoundsForIntegerAndNumber()
    {
        var integer = new OpenApiScalarSchemaEvidence(
            OpenApiScalarSchemaValueKind.Integer,
            minimum: OpenApiSchemaNumber.Parse("0.5"));
        var number = new OpenApiScalarSchemaEvidence(
            OpenApiScalarSchemaValueKind.Number,
            minimum: OpenApiSchemaNumber.Parse("-1e1000"),
            maximum: OpenApiSchemaNumber.Parse("1e1000"));

        Assert.Equal("0.5", integer.Minimum?.ToString());
        Assert.Equal("-1e+1000", number.Minimum?.ToString());
        Assert.Equal("1e+1000", number.Maximum?.ToString());
        Assert.Throws<ArgumentException>(
            () => new OpenApiScalarSchemaEvidence(
                OpenApiScalarSchemaValueKind.Number,
                minimum: OpenApiSchemaNumber.Parse("1.01"),
                maximum: OpenApiSchemaNumber.Parse("1")));
    }
}
