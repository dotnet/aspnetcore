// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

public class HeaderDictionaryTests
{
    public static TheoryData<IEnumerable<string>> HeaderSegmentData => new()
        {
          new[] { "Value1", "Value2", "Value3", "Value4" },
          new[] { "Value1", "", "Value3", "Value4" },
          new[] { "Value1", "", "", "Value4" },
          new[] { "Value1", "", null, "Value4" },
          new[] { "", "", "", "" },
          new[] { "", null, "", null },
        };

    [Fact]
    public void PropertiesAreAccessible()
    {
        var headers = new HeaderDictionary(
            new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
            {
                    { "Header1", "Value1" }
            });

        Assert.Single(headers);
        Assert.Equal<string>(new[] { "Header1" }, headers.Keys);
        Assert.True(headers.ContainsKey("header1"));
        Assert.False(headers.ContainsKey("header2"));
        Assert.Equal("Value1", headers["header1"]);
        Assert.Equal(new[] { "Value1" }, headers["header1"].ToArray());
    }

    [Theory]
    [MemberData(nameof(HeaderSegmentData))]
    public void EmptyHeaderSegmentsAreIgnored(IEnumerable<string> segments)
    {
        var header = string.Join(",", segments);

        var headers = new HeaderDictionary(
           new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
           {
                    { "Header1",  header},
           });

        var result = headers.GetCommaSeparatedValues("Header1");
        var expectedResult = segments.Where(s => !string.IsNullOrEmpty(s));

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void EmptyQuotedHeaderSegmentsAreIgnored()
    {
        var headers = new HeaderDictionary(
           new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
           {
                    { "Header1",  "Value1,\"\",,Value2" },
           });

        var result = headers.GetCommaSeparatedValues("Header1");
        Assert.Equal(new[] { "Value1", "Value2" }, result);
    }

    [Fact]
    public void ReadActionsWorkWhenReadOnly()
    {
        var headers = new HeaderDictionary(
            new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
            {
                    { "Header1", "Value1" }
            });

        headers.IsReadOnly = true;

        Assert.Single(headers);
        Assert.Equal<string>(new[] { "Header1" }, headers.Keys);
        Assert.True(headers.ContainsKey("header1"));
        Assert.False(headers.ContainsKey("header2"));
        Assert.Equal("Value1", headers["header1"]);
        Assert.Equal(new[] { "Value1" }, headers["header1"].ToArray());
    }

    [Fact]
    public void WriteActionsThrowWhenReadOnly()
    {
        var headers = new HeaderDictionary();
        headers.IsReadOnly = true;

        Assert.Throws<InvalidOperationException>(() => headers["header1"] = "value1");
        Assert.Throws<InvalidOperationException>(() => ((IDictionary<string, StringValues>)headers)["header1"] = "value1");
        Assert.Throws<InvalidOperationException>(() => headers.ContentLength = 12);
        Assert.Throws<InvalidOperationException>(() => headers.Add(new KeyValuePair<string, StringValues>("header1", "value1")));
        Assert.Throws<InvalidOperationException>(() => headers.Add("header1", "value1"));
        Assert.Throws<InvalidOperationException>(() => headers.Clear());
        Assert.Throws<InvalidOperationException>(() => headers.Remove(new KeyValuePair<string, StringValues>("header1", "value1")));
        Assert.Throws<InvalidOperationException>(() => headers.Remove("header1"));
    }

    [Fact]
    public void GetCommaSeparatedValues_WorksForUnquotedHeaderValuesEndingWithSpace()
    {
        var headers = new HeaderDictionary
            {
                { "Via", "value " },
            };

        var result = headers.GetCommaSeparatedValues("Via");

        Assert.Equal(new[] { "value " }, result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReturnsCorrectStringValuesEmptyForMissingHeaders(bool withStore)
    {
        // Test both with and without HeaderDictionary.Store set.
        var emptyHeaders = withStore ? new HeaderDictionary(1) : new HeaderDictionary();

        // StringValues.Empty.Equals(default(StringValues)), so we check if the implicit conversion
        // to string[] returns null or Array.Empty<string>() to tell the difference.
        Assert.Same(Array.Empty<string>(), (string[])emptyHeaders["Header1"]);

        IHeaderDictionary asIHeaderDictionary = emptyHeaders;
        Assert.Same(Array.Empty<string>(), (string[])asIHeaderDictionary["Header1"]);
        Assert.Same(Array.Empty<string>(), (string[])asIHeaderDictionary.Host);

        IDictionary<string, StringValues> asIDictionary = emptyHeaders;
        Assert.Throws<KeyNotFoundException>(() => asIDictionary["Header1"]);
    }

    [Theory]
    [InlineData(new[] { "a" }, "a")]
    [InlineData(new[] { "a", "b" }, "a,b")]
    [InlineData(new[] { "a,b" }, "\"a,b\"")]
    [InlineData(new[] { "\"a,b\"" }, "\"a,b\"")]
    [InlineData(new[] { "\"a,b" }, "\"\"a,b\"")]
    [InlineData(new[] { "a,b\"" }, "\"a,b\"\"")]
    [InlineData(new[] { "," }, "\",\"")]
    [InlineData(new[] { "\"" }, "\"")]
    [InlineData(new[] { "a,b", "c", "\"d,e\"" }, "\"a,b\",c,\"d,e\"")]
    [InlineData(new[] { null, "b" }, ",b")]
    [InlineData(new[] { "a", null }, "a,")]
    [InlineData(new[] { "", "" }, ",")]
    [InlineData(new[] { "", "a,b" }, ",\"a,b\"")]
    public void SetCommaSeparatedValues_JoinsAndQuotesValues(string[] values, string expected)
    {
        var headers = new HeaderDictionary();

        headers.SetCommaSeparatedValues("Header1", values);

        Assert.Equal(expected, headers["Header1"].ToString());
        Assert.Equal(1, headers["Header1"].Count);
    }

    public static TheoryData<string[]> EmptyHeaderValues => new()
    {
        Array.Empty<string>(),
        new[] { "" },
        new string[] { null },
    };

    [Theory]
    [MemberData(nameof(EmptyHeaderValues))]
    public void SetCommaSeparatedValues_RemovesHeaderForEmptyValues(string[] values)
    {
        var headers = new HeaderDictionary
        {
            ["Header1"] = "existing"
        };

        headers.SetCommaSeparatedValues("Header1", values);

        Assert.False(headers.ContainsKey("Header1"));
    }

    [Theory]
    [InlineData(new[] { "a" }, "x,a")]
    [InlineData(new[] { "a", "b,c" }, "x,a,\"b,c\"")]
    [InlineData(new[] { "\"b,c\"" }, "x,\"b,c\"")]
    [InlineData(new[] { "" }, "x,")]
    [InlineData(new string[] { null }, "x,")]
    [InlineData(new[] { null, "a" }, "x,,a")]
    public void AppendCommaSeparatedValues_AppendsToExistingValue(string[] values, string expected)
    {
        var headers = new HeaderDictionary
        {
            ["Header1"] = "x"
        };

        headers.AppendCommaSeparatedValues("Header1", values);

        Assert.Equal(expected, headers["Header1"].ToString());
        Assert.Equal(1, headers["Header1"].Count);
    }

    [Fact]
    public void AppendCommaSeparatedValues_AppendsToMultipleExistingValues()
    {
        var headers = new HeaderDictionary
        {
            ["Header1"] = new StringValues(new[] { "x", "y" })
        };

        headers.AppendCommaSeparatedValues("Header1", "a", "b,c");

        Assert.Equal("x,y,a,\"b,c\"", headers["Header1"].ToString());
        Assert.Equal(1, headers["Header1"].Count);
    }

    [Fact]
    public void AppendCommaSeparatedValues_SetsValueWhenHeaderIsMissing()
    {
        var headers = new HeaderDictionary();

        headers.AppendCommaSeparatedValues("Header1", "a", "b,c");
        headers.AppendCommaSeparatedValues("Header2", "");
        headers.AppendCommaSeparatedValues("Header3");

        Assert.Equal("a,\"b,c\"", headers["Header1"].ToString());
        Assert.False(headers.ContainsKey("Header2"));
        Assert.False(headers.ContainsKey("Header3"));
    }
}
