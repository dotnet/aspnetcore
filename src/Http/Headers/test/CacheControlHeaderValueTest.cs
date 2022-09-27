// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class CacheControlHeaderValueTest
{
    [Fact]
    public void Properties_SetAndGetAllProperties_SetValueReturnedInGetter()
    {
        var cacheControl = new CacheControlHeaderValue();

        // Bool properties
        cacheControl.NoCache = true;
        Assert.True(cacheControl.NoCache, "NoCache");
        cacheControl.NoStore = true;
        Assert.True(cacheControl.NoStore, "NoStore");
        cacheControl.MaxStale = true;
        Assert.True(cacheControl.MaxStale, "MaxStale");
        cacheControl.NoTransform = true;
        Assert.True(cacheControl.NoTransform, "NoTransform");
        cacheControl.OnlyIfCached = true;
        Assert.True(cacheControl.OnlyIfCached, "OnlyIfCached");
        cacheControl.Public = true;
        Assert.True(cacheControl.Public, "Public");
        cacheControl.Private = true;
        Assert.True(cacheControl.Private, "Private");
        cacheControl.MustRevalidate = true;
        Assert.True(cacheControl.MustRevalidate, "MustRevalidate");
        cacheControl.ProxyRevalidate = true;
        Assert.True(cacheControl.ProxyRevalidate, "ProxyRevalidate");

        // TimeSpan properties
        TimeSpan timeSpan = new TimeSpan(1, 2, 3);
        cacheControl.MaxAge = timeSpan;
        Assert.Equal(timeSpan, cacheControl.MaxAge);
        cacheControl.SharedMaxAge = timeSpan;
        Assert.Equal(timeSpan, cacheControl.SharedMaxAge);
        cacheControl.MaxStaleLimit = timeSpan;
        Assert.Equal(timeSpan, cacheControl.MaxStaleLimit);
        cacheControl.MinFresh = timeSpan;
        Assert.Equal(timeSpan, cacheControl.MinFresh);

        // String collection properties
        Assert.NotNull(cacheControl.NoCacheHeaders);
        Assert.Throws<ArgumentException>(() => cacheControl.NoCacheHeaders.Add(null));
        Assert.Throws<FormatException>(() => cacheControl.NoCacheHeaders.Add("invalid PLACEHOLDER"));
        cacheControl.NoCacheHeaders.Add("PLACEHOLDER");
        Assert.Equal(1, cacheControl.NoCacheHeaders.Count);
        Assert.Equal("PLACEHOLDER", cacheControl.NoCacheHeaders.First());

        Assert.NotNull(cacheControl.PrivateHeaders);
        Assert.Throws<ArgumentException>(() => cacheControl.PrivateHeaders.Add(null));
        Assert.Throws<FormatException>(() => cacheControl.PrivateHeaders.Add("invalid PLACEHOLDER"));
        cacheControl.PrivateHeaders.Add("PLACEHOLDER");
        Assert.Equal(1, cacheControl.PrivateHeaders.Count);
        Assert.Equal("PLACEHOLDER", cacheControl.PrivateHeaders.First());

        // NameValueHeaderValue collection property
        Assert.NotNull(cacheControl.Extensions);
        Assert.Throws<ArgumentNullException>(() => cacheControl.Extensions.Add(null!));
        cacheControl.Extensions.Add(new NameValueHeaderValue("name", "value"));
        Assert.Equal(1, cacheControl.Extensions.Count);
        Assert.Equal(new NameValueHeaderValue("name", "value"), cacheControl.Extensions.First());
    }

    [Fact]
    public void ToString_UseRequestDirectiveValues_AllSerializedCorrectly()
    {
        var cacheControl = new CacheControlHeaderValue();
        Assert.Equal("", cacheControl.ToString());

        // Note that we allow all combinations of all properties even though the RFC specifies rules what value
        // can be used together.
        // Also for property pairs (bool property + collection property) like 'NoCache' and 'NoCacheHeaders' the
        // caller needs to set the bool property in order for the collection to be populated as string.

        // Cache Request Directive sample
        cacheControl.NoStore = true;
        Assert.Equal("no-store", cacheControl.ToString());
        cacheControl.NoCache = true;
        Assert.Equal("no-store, no-cache", cacheControl.ToString());
        cacheControl.MaxAge = new TimeSpan(0, 1, 10);
        Assert.Equal("no-store, no-cache, max-age=70", cacheControl.ToString());
        cacheControl.MaxStale = true;
        Assert.Equal("no-store, no-cache, max-age=70, max-stale", cacheControl.ToString());
        cacheControl.MaxStaleLimit = new TimeSpan(0, 2, 5);
        Assert.Equal("no-store, no-cache, max-age=70, max-stale=125", cacheControl.ToString());
        cacheControl.MinFresh = new TimeSpan(0, 3, 0);
        Assert.Equal("no-store, no-cache, max-age=70, max-stale=125, min-fresh=180", cacheControl.ToString());

        cacheControl = new CacheControlHeaderValue();
        cacheControl.NoTransform = true;
        Assert.Equal("no-transform", cacheControl.ToString());
        cacheControl.OnlyIfCached = true;
        Assert.Equal("no-transform, only-if-cached", cacheControl.ToString());
        cacheControl.Extensions.Add(new NameValueHeaderValue("custom"));
        cacheControl.Extensions.Add(new NameValueHeaderValue("customName", "customValue"));
        Assert.Equal("no-transform, only-if-cached, custom, customName=customValue", cacheControl.ToString());

        cacheControl = new CacheControlHeaderValue();
        cacheControl.Extensions.Add(new NameValueHeaderValue("custom"));
        Assert.Equal("custom", cacheControl.ToString());
    }

    [Fact]
    public void ToString_UseResponseDirectiveValues_AllSerializedCorrectly()
    {
        var cacheControl = new CacheControlHeaderValue();
        Assert.Equal("", cacheControl.ToString());

        cacheControl.NoCache = true;
        Assert.Equal("no-cache", cacheControl.ToString());
        cacheControl.NoCacheHeaders.Add("PLACEHOLDER1");
        Assert.Equal("no-cache=\"PLACEHOLDER1\"", cacheControl.ToString());
        cacheControl.Public = true;
        Assert.Equal("public, no-cache=\"PLACEHOLDER1\"", cacheControl.ToString());

        cacheControl = new CacheControlHeaderValue();
        cacheControl.Private = true;
        Assert.Equal("private", cacheControl.ToString());
        cacheControl.PrivateHeaders.Add("PLACEHOLDER2");
        cacheControl.PrivateHeaders.Add("PLACEHOLDER3");
        Assert.Equal("private=\"PLACEHOLDER2, PLACEHOLDER3\"", cacheControl.ToString());
        cacheControl.MustRevalidate = true;
        Assert.Equal("must-revalidate, private=\"PLACEHOLDER2, PLACEHOLDER3\"", cacheControl.ToString());
        cacheControl.ProxyRevalidate = true;
        Assert.Equal("must-revalidate, proxy-revalidate, private=\"PLACEHOLDER2, PLACEHOLDER3\"", cacheControl.ToString());
    }

    [Fact]
    public void GetHashCode_CompareValuesWithBoolFieldsSet_MatchExpectation()
    {
        // Verify that different bool fields return different hash values.
        var values = new CacheControlHeaderValue[9];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = new CacheControlHeaderValue();
        }

        values[0].ProxyRevalidate = true;
        values[1].NoCache = true;
        values[2].NoStore = true;
        values[3].MaxStale = true;
        values[4].NoTransform = true;
        values[5].OnlyIfCached = true;
        values[6].Public = true;
        values[7].Private = true;
        values[8].MustRevalidate = true;

        // Only one bool field set. All hash codes should differ
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values.Length; j++)
            {
                if (i != j)
                {
                    CompareHashCodes(values[i], values[j], false);
                }
            }
        }

        // Validate that two instances with the same bool fields set are equal.
        values[0].NoCache = true;
        CompareHashCodes(values[0], values[1], false);
        values[1].ProxyRevalidate = true;
        CompareHashCodes(values[0], values[1], true);
    }

    [Fact]
    public void GetHashCode_CompareValuesWithTimeSpanFieldsSet_MatchExpectation()
    {
        // Verify that different timespan fields return different hash values.
        var values = new CacheControlHeaderValue[4];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = new CacheControlHeaderValue();
        }

        values[0].MaxAge = new TimeSpan(0, 1, 1);
        values[1].MaxStaleLimit = new TimeSpan(0, 1, 1);
        values[2].MinFresh = new TimeSpan(0, 1, 1);
        values[3].SharedMaxAge = new TimeSpan(0, 1, 1);

        // Only one timespan field set. All hash codes should differ
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values.Length; j++)
            {
                if (i != j)
                {
                    CompareHashCodes(values[i], values[j], false);
                }
            }
        }

        values[0].MaxStaleLimit = new TimeSpan(0, 1, 2);
        CompareHashCodes(values[0], values[1], false);

        values[1].MaxAge = new TimeSpan(0, 1, 1);
        values[1].MaxStaleLimit = new TimeSpan(0, 1, 2);
        CompareHashCodes(values[0], values[1], true);
    }

    [Fact]
    public void GetHashCode_CompareCollectionFieldsSet_MatchExpectation()
    {
        var cacheControl1 = new CacheControlHeaderValue();
        var cacheControl2 = new CacheControlHeaderValue();
        var cacheControl3 = new CacheControlHeaderValue();
        var cacheControl4 = new CacheControlHeaderValue();
        var cacheControl5 = new CacheControlHeaderValue();

        cacheControl1.NoCache = true;
        cacheControl1.NoCacheHeaders.Add("PLACEHOLDER2");

        cacheControl2.NoCache = true;
        cacheControl2.NoCacheHeaders.Add("PLACEHOLDER1");
        cacheControl2.NoCacheHeaders.Add("PLACEHOLDER2");

        CompareHashCodes(cacheControl1, cacheControl2, false);

        cacheControl1.NoCacheHeaders.Add("PLACEHOLDER1");
        CompareHashCodes(cacheControl1, cacheControl2, true);

        // Since NoCache and Private generate different hash codes, even if NoCacheHeaders and PrivateHeaders
        // have the same values, the hash code will be different.
        cacheControl3.Private = true;
        cacheControl3.PrivateHeaders.Add("PLACEHOLDER2");
        CompareHashCodes(cacheControl1, cacheControl3, false);

        cacheControl4.Extensions.Add(new NameValueHeaderValue("custom"));
        CompareHashCodes(cacheControl1, cacheControl4, false);

        cacheControl5.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
        cacheControl5.Extensions.Add(new NameValueHeaderValue("custom"));
        CompareHashCodes(cacheControl4, cacheControl5, false);

        cacheControl4.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
        CompareHashCodes(cacheControl4, cacheControl5, true);
    }

    [Fact]
    public void Equals_CompareValuesWithBoolFieldsSet_MatchExpectation()
    {
        // Verify that different bool fields return different hash values.
        var values = new CacheControlHeaderValue[9];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = new CacheControlHeaderValue();
        }

        values[0].ProxyRevalidate = true;
        values[1].NoCache = true;
        values[2].NoStore = true;
        values[3].MaxStale = true;
        values[4].NoTransform = true;
        values[5].OnlyIfCached = true;
        values[6].Public = true;
        values[7].Private = true;
        values[8].MustRevalidate = true;

        // Only one bool field set. All hash codes should differ
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values.Length; j++)
            {
                if (i != j)
                {
                    CompareValues(values[i], values[j], false);
                }
            }
        }

        // Validate that two instances with the same bool fields set are equal.
        values[0].NoCache = true;
        CompareValues(values[0], values[1], false);
        values[1].ProxyRevalidate = true;
        CompareValues(values[0], values[1], true);
    }

    [Fact]
    public void Equals_CompareValuesWithTimeSpanFieldsSet_MatchExpectation()
    {
        // Verify that different timespan fields return different hash values.
        var values = new CacheControlHeaderValue[4];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = new CacheControlHeaderValue();
        }

        values[0].MaxAge = new TimeSpan(0, 1, 1);
        values[1].MaxStaleLimit = new TimeSpan(0, 1, 1);
        values[2].MinFresh = new TimeSpan(0, 1, 1);
        values[3].SharedMaxAge = new TimeSpan(0, 1, 1);

        // Only one timespan field set. All hash codes should differ
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values.Length; j++)
            {
                if (i != j)
                {
                    CompareValues(values[i], values[j], false);
                }
            }
        }

        values[0].MaxStaleLimit = new TimeSpan(0, 1, 2);
        CompareValues(values[0], values[1], false);

        values[1].MaxAge = new TimeSpan(0, 1, 1);
        values[1].MaxStaleLimit = new TimeSpan(0, 1, 2);
        CompareValues(values[0], values[1], true);

        var value1 = new CacheControlHeaderValue();
        value1.MaxStale = true;
        var value2 = new CacheControlHeaderValue();
        value2.MaxStale = true;
        CompareValues(value1, value2, true);

        value2.MaxStaleLimit = new TimeSpan(1, 2, 3);
        CompareValues(value1, value2, false);
    }

    [Fact]
    public void Equals_CompareCollectionFieldsSet_MatchExpectation()
    {
        var cacheControl1 = new CacheControlHeaderValue();
        var cacheControl2 = new CacheControlHeaderValue();
        var cacheControl3 = new CacheControlHeaderValue();
        var cacheControl4 = new CacheControlHeaderValue();
        var cacheControl5 = new CacheControlHeaderValue();
        var cacheControl6 = new CacheControlHeaderValue();

        cacheControl1.NoCache = true;
        cacheControl1.NoCacheHeaders.Add("PLACEHOLDER2");

        Assert.False(cacheControl1.Equals(null), "Compare with 'null'");

        cacheControl2.NoCache = true;
        cacheControl2.NoCacheHeaders.Add("PLACEHOLDER1");
        cacheControl2.NoCacheHeaders.Add("PLACEHOLDER2");

        CompareValues(cacheControl1!, cacheControl2, false);

        cacheControl1!.NoCacheHeaders.Add("PLACEHOLDER1");
        CompareValues(cacheControl1, cacheControl2, true);

        // Since NoCache and Private generate different hash codes, even if NoCacheHeaders and PrivateHeaders
        // have the same values, the hash code will be different.
        cacheControl3.Private = true;
        cacheControl3.PrivateHeaders.Add("PLACEHOLDER2");
        CompareValues(cacheControl1, cacheControl3, false);

        cacheControl4.Private = true;
        cacheControl4.PrivateHeaders.Add("PLACEHOLDER3");
        CompareValues(cacheControl3, cacheControl4, false);

        cacheControl5.Extensions.Add(new NameValueHeaderValue("custom"));
        CompareValues(cacheControl1, cacheControl5, false);

        cacheControl6.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
        cacheControl6.Extensions.Add(new NameValueHeaderValue("custom"));
        CompareValues(cacheControl5, cacheControl6, false);

        cacheControl5.Extensions.Add(new NameValueHeaderValue("customN", "customV"));
        CompareValues(cacheControl5, cacheControl6, true);
    }

    [Fact]
    public void TryParse_DifferentValidScenarios_AllReturnTrue()
    {
        var expected = new CacheControlHeaderValue();
        expected.NoCache = true;
        CheckValidTryParse(" , no-cache ,,", expected);

        expected = new CacheControlHeaderValue();
        expected.NoCache = true;
        expected.NoCacheHeaders.Add("PLACEHOLDER1");
        expected.NoCacheHeaders.Add("PLACEHOLDER2");
        CheckValidTryParse("no-cache=\"PLACEHOLDER1, PLACEHOLDER2\"", expected);

        expected = new CacheControlHeaderValue();
        expected.NoStore = true;
        expected.MaxAge = new TimeSpan(0, 0, 125);
        expected.MaxStale = true;
        CheckValidTryParse(" no-store , max-age = 125, max-stale,", expected);

        expected = new CacheControlHeaderValue();
        expected.MinFresh = new TimeSpan(0, 0, 123);
        expected.NoTransform = true;
        expected.OnlyIfCached = true;
        expected.Extensions.Add(new NameValueHeaderValue("custom"));
        CheckValidTryParse("min-fresh=123, no-transform, only-if-cached, custom", expected);

        expected = new CacheControlHeaderValue();
        expected.Public = true;
        expected.Private = true;
        expected.PrivateHeaders.Add("PLACEHOLDER1");
        expected.MustRevalidate = true;
        expected.ProxyRevalidate = true;
        expected.Extensions.Add(new NameValueHeaderValue("c", "d"));
        expected.Extensions.Add(new NameValueHeaderValue("a", "b"));
        CheckValidTryParse(",public, , private=\"PLACEHOLDER1\", must-revalidate, c=d, proxy-revalidate, a=b", expected);

        expected = new CacheControlHeaderValue();
        expected.Private = true;
        expected.SharedMaxAge = new TimeSpan(0, 0, 1234567890);
        expected.MaxAge = new TimeSpan(0, 0, 987654321);
        CheckValidTryParse("s-maxage=1234567890, private, max-age = 987654321,", expected);

        expected = new CacheControlHeaderValue();
        expected.Extensions.Add(new NameValueHeaderValue("custom", ""));
        CheckValidTryParse("custom=", expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    // PLACEHOLDER-only values
    [InlineData("no-store=15")]
    [InlineData("no-store=")]
    [InlineData("no-transform=a")]
    [InlineData("no-transform=")]
    [InlineData("only-if-cached=\"x\"")]
    [InlineData("only-if-cached=")]
    [InlineData("public=\"x\"")]
    [InlineData("public=")]
    [InlineData("must-revalidate=\"1\"")]
    [InlineData("must-revalidate=")]
    [InlineData("proxy-revalidate=x")]
    [InlineData("proxy-revalidate=")]
    // PLACEHOLDER with optional field-name list
    [InlineData("no-cache=")]
    [InlineData("no-cache=PLACEHOLDER")]
    [InlineData("no-cache=\"PLACEHOLDER")]
    [InlineData("no-cache=\"\"")] // at least one PLACEHOLDER expected as value
    [InlineData("private=")]
    [InlineData("private=PLACEHOLDER")]
    [InlineData("private=\"PLACEHOLDER")]
    [InlineData("private=\",\"")] // at least one PLACEHOLDER expected as value
    [InlineData("private=\"=\"")]
    // PLACEHOLDER with delta-seconds value
    [InlineData("max-age")]
    [InlineData("max-age=")]
    [InlineData("max-age=a")]
    [InlineData("max-age=\"1\"")]
    [InlineData("max-age=1.5")]
    [InlineData("max-stale=")]
    [InlineData("max-stale=a")]
    [InlineData("max-stale=\"1\"")]
    [InlineData("max-stale=1.5")]
    [InlineData("min-fresh")]
    [InlineData("min-fresh=")]
    [InlineData("min-fresh=a")]
    [InlineData("min-fresh=\"1\"")]
    [InlineData("min-fresh=1.5")]
    [InlineData("s-maxage")]
    [InlineData("s-maxage=")]
    [InlineData("s-maxage=a")]
    [InlineData("s-maxage=\"1\"")]
    [InlineData("s-maxage=1.5")]
    // Invalid Extension values
    [InlineData("custom value")]
    public void TryParse_DifferentInvalidScenarios_ReturnsFalse(string input)
    {
        CheckInvalidTryParse(input);
    }

    [Fact]
    public void Parse_SetOfValidValueStrings_ParsedCorrectly()
    {
        // Just verify parser is implemented correctly. Don't try to test syntax parsed by CacheControlHeaderValue.
        var expected = new CacheControlHeaderValue();
        expected.NoStore = true;
        expected.MinFresh = new TimeSpan(0, 2, 3);
        CheckValidParse(" , no-store, min-fresh=123", expected);

        expected = new CacheControlHeaderValue();
        expected.MaxStale = true;
        expected.NoCache = true;
        expected.NoCacheHeaders.Add("t");
        CheckValidParse("max-stale, no-cache=\"t\", ,,", expected);

        expected = new CacheControlHeaderValue();
        expected.Extensions.Add(new NameValueHeaderValue("custom"));
        CheckValidParse("custom =", expected);

        expected = new CacheControlHeaderValue();
        expected.Extensions.Add(new NameValueHeaderValue("custom", ""));
        CheckValidParse("custom =", expected);
    }

    [Fact]
    public void Parse_SetOfInvalidValueStrings_Throws()
    {
        CheckInvalidParse(null);
        CheckInvalidParse("");
        CheckInvalidParse("   ");
        CheckInvalidParse("no-cache,=");
        CheckInvalidParse("max-age=123x");
        CheckInvalidParse("=no-cache");
        CheckInvalidParse("no-cache no-store");
        CheckInvalidParse("会");
    }

    [Fact]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
    {
        // Just verify parser is implemented correctly. Don't try to test syntax parsed by CacheControlHeaderValue.
        var expected = new CacheControlHeaderValue();
        expected.NoStore = true;
        expected.MinFresh = new TimeSpan(0, 2, 3);
        CheckValidTryParse(" , no-store, min-fresh=123", expected);

        expected = new CacheControlHeaderValue();
        expected.MaxStale = true;
        expected.NoCache = true;
        expected.NoCacheHeaders.Add("t");
        CheckValidTryParse("max-stale, no-cache=\"t\", ,,", expected);

        expected = new CacheControlHeaderValue();
        expected.Extensions.Add(new NameValueHeaderValue("custom"));
        CheckValidTryParse("custom = ", expected);

        expected = new CacheControlHeaderValue();
        expected.Extensions.Add(new NameValueHeaderValue("custom", ""));
        CheckValidTryParse("custom =", expected);
    }

    [Fact]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
    {
        CheckInvalidTryParse("no-cache,=");
        CheckInvalidTryParse("max-age=123x");
        CheckInvalidTryParse("=no-cache");
        CheckInvalidTryParse("no-cache no-store");
        CheckInvalidTryParse("会");
    }

    #region Helper methods

    private void CompareHashCodes(CacheControlHeaderValue x, CacheControlHeaderValue y, bool areEqual)
    {
        if (areEqual)
        {
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
        }
        else
        {
            Assert.NotEqual(x.GetHashCode(), y.GetHashCode());
        }
    }

    private void CompareValues(CacheControlHeaderValue x, CacheControlHeaderValue y, bool areEqual)
    {
        Assert.Equal(areEqual, x.Equals(y));
        Assert.Equal(areEqual, y.Equals(x));
    }

    private void CheckValidParse(string? input, CacheControlHeaderValue expectedResult)
    {
        var result = CacheControlHeaderValue.Parse(input);
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidParse(string? input)
    {
        Assert.Throws<FormatException>(() => CacheControlHeaderValue.Parse(input));
    }

    private void CheckValidTryParse(string? input, CacheControlHeaderValue expectedResult)
    {
        Assert.True(CacheControlHeaderValue.TryParse(input, out var result));
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidTryParse(string? input)
    {
        Assert.False(CacheControlHeaderValue.TryParse(input, out var result));
        Assert.Null(result);
    }

    #endregion
}
