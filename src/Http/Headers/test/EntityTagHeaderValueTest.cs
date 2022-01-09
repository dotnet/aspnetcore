// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class EntityTagHeaderValueTest
{
    [Fact]
    public void Ctor_ETagNull_Throw()
    {
        Assert.Throws<ArgumentException>(() => new EntityTagHeaderValue(null));
        // null and empty should be treated the same. So we also throw for empty strings.
        Assert.Throws<ArgumentException>(() => new EntityTagHeaderValue(string.Empty));
    }

    [Fact]
    public void Ctor_ETagInvalidFormat_ThrowFormatException()
    {
        // When adding values using strongly typed objects, no leading/trailing LWS (whitespaces) are allowed.
        AssertFormatException("tag");
        AssertFormatException(" tag ");
        AssertFormatException("\"tag\" invalid");
        AssertFormatException("\"tag");
        AssertFormatException("tag\"");
        AssertFormatException("\"tag\"\"");
        AssertFormatException("\"\"tag\"\"");
        AssertFormatException("\"\"tag\"");
        AssertFormatException("W/\"tag\""); // tag value must not contain 'W/'
    }

    [Fact]
    public void Ctor_ETagValidFormat_SuccessfullyCreated()
    {
        var etag = new EntityTagHeaderValue("\"tag\"");
        Assert.Equal("\"tag\"", etag.Tag);
        Assert.False(etag.IsWeak, "IsWeak");
    }

    [Fact]
    public void Ctor_ETagValidFormatAndIsWeak_SuccessfullyCreated()
    {
        var etag = new EntityTagHeaderValue("\"e tag\"", true);
        Assert.Equal("\"e tag\"", etag.Tag);
        Assert.True(etag.IsWeak, "IsWeak");
    }

    [Fact]
    public void ToString_UseDifferentETags_AllSerializedCorrectly()
    {
        var etag = new EntityTagHeaderValue("\"e tag\"");
        Assert.Equal("\"e tag\"", etag.ToString());

        etag = new EntityTagHeaderValue("\"e tag\"", true);
        Assert.Equal("W/\"e tag\"", etag.ToString());

        etag = new EntityTagHeaderValue("\"\"", false);
        Assert.Equal("\"\"", etag.ToString());
    }

    [Fact]
    public void GetHashCode_UseSameAndDifferentETags_SameOrDifferentHashCodes()
    {
        var etag1 = new EntityTagHeaderValue("\"tag\"");
        var etag2 = new EntityTagHeaderValue("\"TAG\"");
        var etag3 = new EntityTagHeaderValue("\"tag\"", true);
        var etag4 = new EntityTagHeaderValue("\"tag1\"");
        var etag5 = new EntityTagHeaderValue("\"tag\"");
        var etag6 = EntityTagHeaderValue.Any;

        Assert.NotEqual(etag1.GetHashCode(), etag2.GetHashCode());
        Assert.NotEqual(etag1.GetHashCode(), etag3.GetHashCode());
        Assert.NotEqual(etag1.GetHashCode(), etag4.GetHashCode());
        Assert.NotEqual(etag1.GetHashCode(), etag6.GetHashCode());
        Assert.Equal(etag1.GetHashCode(), etag5.GetHashCode());
    }

    [Fact]
    public void Equals_UseSameAndDifferentETags_EqualOrNotEqualNoExceptions()
    {
        var etag1 = new EntityTagHeaderValue("\"tag\"");
        var etag2 = new EntityTagHeaderValue("\"TAG\"");
        var etag3 = new EntityTagHeaderValue("\"tag\"", true);
        var etag4 = new EntityTagHeaderValue("\"tag1\"");
        var etag5 = new EntityTagHeaderValue("\"tag\"");
        var etag6 = EntityTagHeaderValue.Any;

        Assert.False(etag1.Equals(etag2), "Different casing.");
        Assert.False(etag2.Equals(etag1), "Different casing.");
        Assert.False(etag1.Equals(null), "tag vs. <null>.");
        Assert.False(etag1!.Equals(etag3), "strong vs. weak.");
        Assert.False(etag3.Equals(etag1), "weak vs. strong.");
        Assert.False(etag1.Equals(etag4), "tag vs. tag1.");
        Assert.False(etag1.Equals(etag6), "tag vs. *.");
        Assert.True(etag1.Equals(etag5), "tag vs. tag..");
    }

    [Fact]
    public void Compare_WithNull_ReturnsFalse()
    {
        Assert.False(EntityTagHeaderValue.Any.Compare(null, useStrongComparison: true));
        Assert.False(EntityTagHeaderValue.Any.Compare(null, useStrongComparison: false));
    }

    public static TheoryData<EntityTagHeaderValue, EntityTagHeaderValue> NotEquivalentUnderStrongComparison
    {
        get
        {
            return new TheoryData<EntityTagHeaderValue, EntityTagHeaderValue>
                {
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"TAG\"") },
                    { new EntityTagHeaderValue("\"tag\"", true), new EntityTagHeaderValue("\"tag\"", true) },
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"tag\"", true) },
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"tag1\"") },
                    { new EntityTagHeaderValue("\"tag\""), EntityTagHeaderValue.Any },
                };
        }
    }

    [Theory]
    [MemberData(nameof(NotEquivalentUnderStrongComparison))]
    public void CompareUsingStrongComparison_NonEquivalentPairs_ReturnFalse(EntityTagHeaderValue left, EntityTagHeaderValue right)
    {
        Assert.False(left.Compare(right, useStrongComparison: true));
        Assert.False(right.Compare(left, useStrongComparison: true));
    }

    public static TheoryData<EntityTagHeaderValue, EntityTagHeaderValue> EquivalentUnderStrongComparison
    {
        get
        {
            return new TheoryData<EntityTagHeaderValue, EntityTagHeaderValue>
                {
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"tag\"") },
                };
        }
    }

    [Theory]
    [MemberData(nameof(EquivalentUnderStrongComparison))]
    public void CompareUsingStrongComparison_EquivalentPairs_ReturnTrue(EntityTagHeaderValue left, EntityTagHeaderValue right)
    {
        Assert.True(left.Compare(right, useStrongComparison: true));
        Assert.True(right.Compare(left, useStrongComparison: true));
    }

    public static TheoryData<EntityTagHeaderValue, EntityTagHeaderValue> NotEquivalentUnderWeakComparison
    {
        get
        {
            return new TheoryData<EntityTagHeaderValue, EntityTagHeaderValue>
                {
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"TAG\"") },
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"tag1\"") },
                    { new EntityTagHeaderValue("\"tag\""), EntityTagHeaderValue.Any },
                };
        }
    }

    [Theory]
    [MemberData(nameof(NotEquivalentUnderWeakComparison))]
    public void CompareUsingWeakComparison_NonEquivalentPairs_ReturnFalse(EntityTagHeaderValue left, EntityTagHeaderValue right)
    {
        Assert.False(left.Compare(right, useStrongComparison: false));
        Assert.False(right.Compare(left, useStrongComparison: false));
    }

    public static TheoryData<EntityTagHeaderValue, EntityTagHeaderValue> EquivalentUnderWeakComparison
    {
        get
        {
            return new TheoryData<EntityTagHeaderValue, EntityTagHeaderValue>
                {
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"tag\"") },
                    { new EntityTagHeaderValue("\"tag\"", true), new EntityTagHeaderValue("\"tag\"", true) },
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"tag\"", true) },
                };
        }
    }

    [Theory]
    [MemberData(nameof(EquivalentUnderWeakComparison))]
    public void CompareUsingWeakComparison_EquivalentPairs_ReturnTrue(EntityTagHeaderValue left, EntityTagHeaderValue right)
    {
        Assert.True(left.Compare(right, useStrongComparison: false));
        Assert.True(right.Compare(left, useStrongComparison: false));
    }

    [Fact]
    public void Parse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidParse("\"tag\"", new EntityTagHeaderValue("\"tag\""));
        CheckValidParse(" \"tag\" ", new EntityTagHeaderValue("\"tag\""));
        CheckValidParse("\r\n \"tag\"\r\n ", new EntityTagHeaderValue("\"tag\""));
        CheckValidParse("\"tag\"", new EntityTagHeaderValue("\"tag\""));
        CheckValidParse("\"tag会\"", new EntityTagHeaderValue("\"tag会\""));
        CheckValidParse("W/\"tag\"", new EntityTagHeaderValue("\"tag\"", true));
        CheckValidParse("*", new EntityTagHeaderValue("*"));
    }

    [Fact]
    public void Parse_SetOfInvalidValueStrings_Throws()
    {
        CheckInvalidParse(null);
        CheckInvalidParse(string.Empty);
        CheckInvalidParse("  ");
        CheckInvalidParse("  !");
        CheckInvalidParse("tag\"  !");
        CheckInvalidParse("!\"tag\"");
        CheckInvalidParse("\"tag\",");
        CheckInvalidParse("W");
        CheckInvalidParse("W/");
        CheckInvalidParse("W/\"");
        CheckInvalidParse("\"tag\" \"tag2\"");
        CheckInvalidParse("/\"tag\"");
    }

    [Fact]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidTryParse("\"tag\"", new EntityTagHeaderValue("\"tag\""));
        CheckValidTryParse(" \"tag\" ", new EntityTagHeaderValue("\"tag\""));
        CheckValidTryParse("\r\n \"tag\"\r\n ", new EntityTagHeaderValue("\"tag\""));
        CheckValidTryParse("\"tag\"", new EntityTagHeaderValue("\"tag\""));
        CheckValidTryParse("\"tag会\"", new EntityTagHeaderValue("\"tag会\""));
        CheckValidTryParse("W/\"tag\"", new EntityTagHeaderValue("\"tag\"", true));
        CheckValidTryParse("*", new EntityTagHeaderValue("*"));
    }

    [Fact]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
    {
        CheckInvalidTryParse(null);
        CheckInvalidTryParse(string.Empty);
        CheckInvalidTryParse("  ");
        CheckInvalidTryParse("  !");
        CheckInvalidTryParse("tag\"  !");
        CheckInvalidTryParse("!\"tag\"");
        CheckInvalidTryParse("\"tag\",");
        CheckInvalidTryParse("\"tag\" \"tag2\"");
        CheckInvalidTryParse("/\"tag\"");
    }

    [Fact]
    public void ParseList_NullOrEmptyArray_ReturnsEmptyList()
    {
        var result = EntityTagHeaderValue.ParseList(null);
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);

        result = EntityTagHeaderValue.ParseList(new string[0]);
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);

        result = EntityTagHeaderValue.ParseList(new string[] { "" });
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void TryParseList_NullOrEmptyArray_ReturnsFalse()
    {
        Assert.False(EntityTagHeaderValue.TryParseList(null, out var results));
        Assert.False(EntityTagHeaderValue.TryParseList(new string[0], out results));
        Assert.False(EntityTagHeaderValue.TryParseList(new string[] { "" }, out results));
    }

    [Fact]
    public void ParseList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "\"tag\"",
                "",
                " \"tag\" ",
                "\r\n \"tag\"\r\n ",
                "\"tag会\"",
                "\"tag\",\"tag\"",
                "\"tag\", \"tag\"",
                "W/\"tag\"",
            };
        IList<EntityTagHeaderValue> results = EntityTagHeaderValue.ParseList(inputs);

        var expectedResults = new[]
        {
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag会\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\"", true),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseStrictList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "\"tag\"",
                "",
                " \"tag\" ",
                "\r\n \"tag\"\r\n ",
                "\"tag会\"",
                "\"tag\",\"tag\"",
                "\"tag\", \"tag\"",
                "W/\"tag\"",
            };
        IList<EntityTagHeaderValue> results = EntityTagHeaderValue.ParseStrictList(inputs);

        var expectedResults = new[]
        {
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag会\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\"", true),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "\"tag\"",
                "",
                " \"tag\" ",
                "\r\n \"tag\"\r\n ",
                "\"tag会\"",
                "\"tag\",\"tag\"",
                "\"tag\", \"tag\"",
                "W/\"tag\"",
            };
        Assert.True(EntityTagHeaderValue.TryParseList(inputs, out var results));
        var expectedResults = new[]
        {
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag会\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\"", true),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseStrictList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "\"tag\"",
                "",
                " \"tag\" ",
                "\r\n \"tag\"\r\n ",
                "\"tag会\"",
                "\"tag\",\"tag\"",
                "\"tag\", \"tag\"",
                "W/\"tag\"",
            };
        Assert.True(EntityTagHeaderValue.TryParseStrictList(inputs, out var results));
        var expectedResults = new[]
        {
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag会\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\"", true),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseList_WithSomeInvalidValues_ExcludesInvalidValues()
    {
        var inputs = new[]
        {
                "",
                "\"tag\", tag, \"tag\"",
                "tag, \"tag\"",
                "",
                " \"tag ",
                "\r\n tag\"\r\n ",
                "\"tag会\"",
                "\"tag\", \"tag\"",
                "W/\"tag\"",
            };
        var results = EntityTagHeaderValue.ParseList(inputs);
        var expectedResults = new[]
        {
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag会\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\"", true),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseStrictList_WithSomeInvalidValues_Throws()
    {
        var inputs = new[]
        {
                "",
                "\"tag\", tag, \"tag\"",
                "tag, \"tag\"",
                "",
                " \"tag ",
                "\r\n tag\"\r\n ",
                "\"tag会\"",
                "\"tag\", \"tag\"",
                "W/\"tag\"",
            };
        Assert.Throws<FormatException>(() => EntityTagHeaderValue.ParseStrictList(inputs));
    }

    [Fact]
    public void TryParseList_WithSomeInvalidValues_ExcludesInvalidValues()
    {
        var inputs = new[]
        {
                "",
                "\"tag\", tag, \"tag\"",
                "tag, \"tag\"",
                "",
                " \"tag ",
                "\r\n tag\"\r\n ",
                "\"tag会\"",
                "\"tag\", \"tag\"",
                "W/\"tag\"",
            };
        Assert.True(EntityTagHeaderValue.TryParseList(inputs, out var results));
        var expectedResults = new[]
        {
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag会\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\""),
                new EntityTagHeaderValue("\"tag\"", true),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseStrictList_WithSomeInvalidValues_ReturnsFalse()
    {
        var inputs = new[]
        {
                "",
                "\"tag\", tag, \"tag\"",
                "tag, \"tag\"",
                "",
                " \"tag ",
                "\r\n tag\"\r\n ",
                "\"tag会\"",
                "\"tag\", \"tag\"",
                "W/\"tag\"",
            };
        Assert.False(EntityTagHeaderValue.TryParseStrictList(inputs, out var results));
    }

    private void CheckValidParse(string? input, EntityTagHeaderValue expectedResult)
    {
        var result = EntityTagHeaderValue.Parse(input);
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidParse(string? input)
    {
        Assert.Throws<FormatException>(() => EntityTagHeaderValue.Parse(input));
    }

    private void CheckValidTryParse(string? input, EntityTagHeaderValue expectedResult)
    {
        Assert.True(EntityTagHeaderValue.TryParse(input, out var result));
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidTryParse(string? input)
    {
        Assert.False(EntityTagHeaderValue.TryParse(input, out var result));
        Assert.Null(result);
    }

    private static void AssertFormatException(string tag)
    {
        Assert.Throws<FormatException>(() => new EntityTagHeaderValue(tag));
    }
}
