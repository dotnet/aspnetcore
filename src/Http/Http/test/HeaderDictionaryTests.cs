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
}
