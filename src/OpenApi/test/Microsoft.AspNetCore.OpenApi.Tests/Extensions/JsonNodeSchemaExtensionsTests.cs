// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public static class JsonNodeSchemaExtensionsTests
{
    public static TheoryData<string, bool, RangeAttribute, string, string> TestCases()
    {
        bool[] isExclusive = [false, true];

        string[] invariantOrEnglishCultures =
        [
            string.Empty,
            "en",
            "en-AU",
            "en-GB",
            "en-US",
        ];

        string[] commaForDecimalCultures =
        [
            "de-DE",
            "fr-FR",
            "sv-SE",
        ];

        Type[] fractionNumberTypes =
        [
            typeof(float),
            typeof(double),
            typeof(decimal),
        ];

        var testCases = new TheoryData<string, bool, RangeAttribute, string, string>();

        foreach (var culture in invariantOrEnglishCultures)
        {
            foreach (var exclusive in isExclusive)
            {
                testCases.Add(culture, exclusive, new(1, 1234) { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "1", "1234");
                testCases.Add(culture, exclusive, new(1d, 1234d) { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "1", "1234");
                testCases.Add(culture, exclusive, new(1.23, 4.56) { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "1.23", "4.56");

                foreach (var type in fractionNumberTypes)
                {
                    testCases.Add(culture, exclusive, new(type, "1.23", "4.56") { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "1.23", "4.56");
                    testCases.Add(culture, exclusive, new(type, "1.23", "4.56") { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive, ParseLimitsInInvariantCulture = true }, "1.23", "4.56");
                }
            }
        }

        foreach (var culture in commaForDecimalCultures)
        {
            foreach (var exclusive in isExclusive)
            {
                testCases.Add(culture, exclusive, new(1, 1234) { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "1", "1234");
                testCases.Add(culture, exclusive, new(1d, 1234d) { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "1", "1234");
                testCases.Add(culture, exclusive, new(1.23, 4.56) { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "1.23", "4.56");

                foreach (var type in fractionNumberTypes)
                {
                    testCases.Add(culture, exclusive, new(type, "1,23", "4,56") { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "1.23", "4.56");
                    testCases.Add(culture, exclusive, new(type, "1.23", "4.56") { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive, ParseLimitsInInvariantCulture = true }, "1.23", "4.56");
                }
            }
        }

        // Numbers using numeric format, such as with thousands separators
        testCases.Add("en-GB", false, new(typeof(float), "-12,445.7", "12,445.7"), "-12445.7", "12445.7");
        testCases.Add("fr-FR", false, new(typeof(float), "-12 445,7", "12 445,7"), "-12445.7", "12445.7");
        testCases.Add("sv-SE", false, new(typeof(float), "-12 445,7", "12 445,7"), "-12445.7", "12445.7");

        // Decimal value that would lose precision if parsed as a float or double
        foreach (var exclusive in isExclusive)
        {
            testCases.Add("en-US", exclusive, new(typeof(decimal), "12345678901234567890.123456789", "12345678901234567890.123456789") { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive }, "12345678901234567890.123456789", "12345678901234567890.123456789");
            testCases.Add("en-US", exclusive, new(typeof(decimal), "12345678901234567890.123456789", "12345678901234567890.123456789") { MaximumIsExclusive = exclusive, MinimumIsExclusive = exclusive, ParseLimitsInInvariantCulture = true }, "12345678901234567890.123456789", "12345678901234567890.123456789");
        }

        return testCases;
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public static void ApplyValidationAttributes_Handles_RangeAttribute_Correctly(
        string cultureName,
        bool isExclusive,
        RangeAttribute rangeAttribute,
        string expectedMinimum,
        string expectedMaximum)
    {
        // Arrange
        var minimum = decimal.Parse(expectedMinimum, CultureInfo.InvariantCulture);
        var maximum = decimal.Parse(expectedMaximum, CultureInfo.InvariantCulture);

        var schema = new JsonObject();

        // Act
        var previous = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            schema.ApplyValidationAttributes([rangeAttribute]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        // Assert
        if (isExclusive)
        {
            Assert.Equal(minimum, schema["exclusiveMinimum"].GetValue<decimal>());
            Assert.Equal(maximum, schema["exclusiveMaximum"].GetValue<decimal>());
            Assert.False(schema.TryGetPropertyValue("minimum", out _));
            Assert.False(schema.TryGetPropertyValue("maximum", out _));
        }
        else
        {
            Assert.Equal(minimum, schema["minimum"].GetValue<decimal>());
            Assert.Equal(maximum, schema["maximum"].GetValue<decimal>());
            Assert.False(schema.TryGetPropertyValue("exclusiveMinimum", out _));
            Assert.False(schema.TryGetPropertyValue("exclusiveMaximum", out _));
        }
    }

    [Fact]
    public static void ApplyValidationAttributes_Handles_Invalid_RangeAttribute_Values()
    {
        // Arrange
        var rangeAttribute = new RangeAttribute(typeof(int), "foo", "bar");
        var schema = new JsonObject();

        // Act
        schema.ApplyValidationAttributes([rangeAttribute]);

        // Assert
        Assert.False(schema.TryGetPropertyValue("minimum", out _));
        Assert.False(schema.TryGetPropertyValue("maximum", out _));
        Assert.False(schema.TryGetPropertyValue("exclusiveMinimum", out _));
        Assert.False(schema.TryGetPropertyValue("exclusiveMaximum", out _));
    }
}
