// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class CookieHeaderValueTest
{
    public static TheoryData<CookieHeaderValue, string> CookieHeaderDataSet
    {
        get
        {
            var dataset = new TheoryData<CookieHeaderValue, string>();
            var header1 = new CookieHeaderValue("name1", "n1=v1&n2=v2&n3=v3");
            dataset.Add(header1, "name1=n1=v1&n2=v2&n3=v3");

            var header2 = new CookieHeaderValue("name2", "");
            dataset.Add(header2, "name2=");

            var header3 = new CookieHeaderValue("name3", "value3");
            dataset.Add(header3, "name3=value3");

            var header4 = new CookieHeaderValue("name4", "\"value4\"");
            dataset.Add(header4, "name4=\"value4\"");

            return dataset;
        }
    }

    public static TheoryData<string> InvalidCookieHeaderDataSet
    {
        get
        {
            return new TheoryData<string>
                {
                    "=value",
                    "name=value;",
                    "name=value,",
                };
        }
    }

    public static TheoryData<string> InvalidCookieNames
    {
        get
        {
            return new TheoryData<string>
                {
                    "<acb>",
                    "{acb}",
                    "[acb]",
                    "\"acb\"",
                    "a,b",
                    "a;b",
                    "a\\b",
                    "a b",
                };
        }
    }

    public static TheoryData<string> InvalidCookieValues
    {
        get
        {
            return new TheoryData<string>
                {
                    { "\"" },
                    { "a,b" },
                    { "a;b" },
                    { "a\\b" },
                    { "\"abc" },
                    { "a\"bc" },
                    { "abc\"" },
                    { "a b" },
                };
        }
    }

    public static TheoryData<IList<CookieHeaderValue>, string?[]> ListOfCookieHeaderDataSet
    {
        get
        {
            var dataset = new TheoryData<IList<CookieHeaderValue>, string?[]>();
            var header1 = new CookieHeaderValue("name1", "n1=v1&n2=v2&n3=v3");
            var string1 = "name1=n1=v1&n2=v2&n3=v3";

            var header2 = new CookieHeaderValue("name2", "value2");
            var string2 = "name2=value2";

            var header3 = new CookieHeaderValue("name3", "value3");
            var string3 = "name3=value3";

            var header4 = new CookieHeaderValue("name4", "\"value4\"");
            var string4 = "name4=\"value4\"";

            dataset.Add(new[] { header1 }.ToList(), new[] { string1 });
            dataset.Add(new[] { header1, header1 }.ToList(), new[] { string1, string1 });
            dataset.Add(new[] { header1, header1 }.ToList(), new[] { string1, null, "", " ", ";", " , ", string1 });
            dataset.Add(new[] { header2 }.ToList(), new[] { string2 });
            dataset.Add(new[] { header1, header2 }.ToList(), new[] { string1, string2 });
            dataset.Add(new[] { header1, header2 }.ToList(), new[] { string1 + ", " + string2 });
            dataset.Add(new[] { header2, header1 }.ToList(), new[] { string2 + "; " + string1 });
            dataset.Add(new[] { header1, header2, header3, header4 }.ToList(), new[] { string1, string2, string3, string4 });
            dataset.Add(new[] { header1, header2, header3, header4 }.ToList(), new[] { string.Join(",", string1, string2, string3, string4) });
            dataset.Add(new[] { header1, header2, header3, header4 }.ToList(), new[] { string.Join(";", string1, string2, string3, string4) });

            return dataset;
        }
    }

    public static TheoryData<IList<CookieHeaderValue>?, string?[]> ListWithInvalidCookieHeaderDataSet
    {
        get
        {
            var dataset = new TheoryData<IList<CookieHeaderValue>?, string?[]>();
            var header1 = new CookieHeaderValue("name1", "n1=v1&n2=v2&n3=v3");
            var validString1 = "name1=n1=v1&n2=v2&n3=v3";

            var header2 = new CookieHeaderValue("name2", "value2");
            var validString2 = "name2=value2";

            var header3 = new CookieHeaderValue("name3", "value3");
            var validString3 = "name3=value3";

            var invalidString1 = "ipt={\"v\":{\"L\":3},\"pt\":{\"d\":3},ct\":{},\"_t\":44,\"_v\":\"2\"}";

            dataset.Add(null, new[] { invalidString1 });
            dataset.Add(new[] { header1 }.ToList(), new[] { validString1, invalidString1 });
            dataset.Add(new[] { header1 }.ToList(), new[] { validString1, null, "", " ", ";", " , ", invalidString1 });
            dataset.Add(new[] { header1 }.ToList(), new[] { invalidString1, null, "", " ", ";", " , ", validString1 });
            dataset.Add(new[] { header1 }.ToList(), new[] { validString1 + ", " + invalidString1 });
            dataset.Add(new[] { header2 }.ToList(), new[] { invalidString1 + ", " + validString2 });
            dataset.Add(new[] { header1 }.ToList(), new[] { invalidString1 + "; " + validString1 });
            dataset.Add(new[] { header2 }.ToList(), new[] { validString2 + "; " + invalidString1 });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { invalidString1, validString1, validString2, validString3 });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { validString1, invalidString1, validString2, validString3 });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { validString1, validString2, invalidString1, validString3 });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { validString1, validString2, validString3, invalidString1 });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { string.Join(",", invalidString1, validString1, validString2, validString3) });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { string.Join(",", validString1, invalidString1, validString2, validString3) });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { string.Join(",", validString1, validString2, invalidString1, validString3) });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { string.Join(",", validString1, validString2, validString3, invalidString1) });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { string.Join(";", invalidString1, validString1, validString2, validString3) });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { string.Join(";", validString1, invalidString1, validString2, validString3) });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { string.Join(";", validString1, validString2, invalidString1, validString3) });
            dataset.Add(new[] { header1, header2, header3 }.ToList(), new[] { string.Join(";", validString1, validString2, validString3, invalidString1) });

            return dataset;
        }
    }

    [Fact]
    public void CookieHeaderValue_CtorThrowsOnNullName()
    {
        Assert.Throws<ArgumentNullException>(() => new CookieHeaderValue(null, "value"));
    }

    [Theory]
    [MemberData(nameof(InvalidCookieNames))]
    public void CookieHeaderValue_CtorThrowsOnInvalidName(string name)
    {
        Assert.Throws<ArgumentException>(() => new CookieHeaderValue(name, "value"));
    }

    [Theory]
    [MemberData(nameof(InvalidCookieValues))]
    public void CookieHeaderValue_CtorThrowsOnInvalidValue(string value)
    {
        Assert.Throws<ArgumentException>(() => new CookieHeaderValue("name", value));
    }

    [Fact]
    public void CookieHeaderValue_Ctor1_InitializesCorrectly()
    {
        var header = new CookieHeaderValue("cookie");
        Assert.Equal("cookie", header.Name.AsSpan());
        Assert.Equal(string.Empty, header.Value.AsSpan());
    }

    [Theory]
    [InlineData("name", "")]
    [InlineData("name", "value")]
    [InlineData("name", "\"acb\"")]
    public void CookieHeaderValue_Ctor2InitializesCorrectly(string name, string value)
    {
        var header = new CookieHeaderValue(name, value);
        Assert.Equal(name, header.Name.AsSpan());
        Assert.Equal(value, header.Value.AsSpan());
    }

    [Fact]
    public void CookieHeaderValue_Value()
    {
        var cookie = new CookieHeaderValue("name");
        Assert.Equal(string.Empty, cookie.Value.AsSpan());

        cookie.Value = "value1";
        Assert.Equal("value1", cookie.Value.AsSpan());
    }

    [Theory]
    [MemberData(nameof(CookieHeaderDataSet))]
    public void CookieHeaderValue_ToString(CookieHeaderValue input, string expectedValue)
    {
        Assert.Equal(expectedValue, input.ToString());
    }

    [Theory]
    [MemberData(nameof(CookieHeaderDataSet))]
    public void CookieHeaderValue_Parse_AcceptsValidValues(CookieHeaderValue cookie, string expectedValue)
    {
        var header = CookieHeaderValue.Parse(expectedValue);

        Assert.Equal(cookie, header);
        Assert.Equal(expectedValue, header.ToString());
    }

    [Theory]
    [MemberData(nameof(CookieHeaderDataSet))]
    public void CookieHeaderValue_TryParse_AcceptsValidValues(CookieHeaderValue cookie, string expectedValue)
    {
        Assert.True(CookieHeaderValue.TryParse(expectedValue, out var header));

        Assert.Equal(cookie, header);
        Assert.Equal(expectedValue, header!.ToString());
    }

    [Theory]
    [MemberData(nameof(InvalidCookieHeaderDataSet))]
    public void CookieHeaderValue_Parse_RejectsInvalidValues(string value)
    {
        Assert.Throws<FormatException>(() => CookieHeaderValue.Parse(value));
    }

    [Theory]
    [MemberData(nameof(InvalidCookieHeaderDataSet))]
    public void CookieHeaderValue_TryParse_RejectsInvalidValues(string value)
    {
        Assert.False(CookieHeaderValue.TryParse(value, out var _));
    }

    [Theory]
    [MemberData(nameof(ListOfCookieHeaderDataSet))]
    public void CookieHeaderValue_ParseList_AcceptsValidValues(IList<CookieHeaderValue> cookies, string[] input)
    {
        var results = CookieHeaderValue.ParseList(input);

        Assert.Equal(cookies, results);
    }

    [Theory]
    [MemberData(nameof(ListOfCookieHeaderDataSet))]
    public void CookieHeaderValue_ParseStrictList_AcceptsValidValues(IList<CookieHeaderValue> cookies, string[] input)
    {
        var results = CookieHeaderValue.ParseStrictList(input);

        Assert.Equal(cookies, results);
    }

    [Theory]
    [MemberData(nameof(ListOfCookieHeaderDataSet))]
    public void CookieHeaderValue_TryParseList_AcceptsValidValues(IList<CookieHeaderValue> cookies, string[] input)
    {
        var result = CookieHeaderValue.TryParseList(input, out var results);
        Assert.True(result);

        Assert.Equal(cookies, results);
    }

    [Theory]
    [MemberData(nameof(ListOfCookieHeaderDataSet))]
    public void CookieHeaderValue_TryParseStrictList_AcceptsValidValues(IList<CookieHeaderValue> cookies, string[] input)
    {
        var result = CookieHeaderValue.TryParseStrictList(input, out var results);
        Assert.True(result);

        Assert.Equal(cookies, results);
    }

    [Theory]
    [MemberData(nameof(ListWithInvalidCookieHeaderDataSet))]
    public void CookieHeaderValue_ParseList_ExcludesInvalidValues(IList<CookieHeaderValue>? cookies, string[] input)
    {
        var results = CookieHeaderValue.ParseList(input);
        // ParseList always returns a list, even if empty. TryParseList may return null (via out).
        Assert.Equal(cookies ?? new List<CookieHeaderValue>(), results);
    }

    [Theory]
    [MemberData(nameof(ListWithInvalidCookieHeaderDataSet))]
    public void CookieHeaderValue_TryParseList_ExcludesInvalidValues(IList<CookieHeaderValue>? cookies, string[] input)
    {
        var result = CookieHeaderValue.TryParseList(input, out var results);
        Assert.Equal(cookies, results);
        Assert.Equal(cookies?.Count > 0, result);
    }

    [Theory]
    [MemberData(nameof(ListWithInvalidCookieHeaderDataSet))]
    public void CookieHeaderValue_ParseStrictList_ThrowsForAnyInvalidValues(
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
            IList<CookieHeaderValue>? cookies,
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
            string[] input)
    {
        Assert.Throws<FormatException>(() => CookieHeaderValue.ParseStrictList(input));
    }

    [Theory]
    [MemberData(nameof(ListWithInvalidCookieHeaderDataSet))]
    public void CookieHeaderValue_TryParseStrictList_FailsForAnyInvalidValues(
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
            IList<CookieHeaderValue>? cookies,
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
            string[] input)
    {
        var result = CookieHeaderValue.TryParseStrictList(input, out var results);
        Assert.Null(results);
        Assert.False(result);
    }
}
