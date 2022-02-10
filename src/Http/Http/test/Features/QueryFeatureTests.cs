// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

public class QueryFeatureTests
{
    [Fact]
    public void QueryReturnsParsedQueryCollection()
    {
        // Arrange
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "foo=bar" };

        var provider = new QueryFeature(features);

        // Act
        var queryCollection = provider.Query;

        // Assert
        Assert.Equal("bar", queryCollection["foo"]);
    }

    [Theory]
    [InlineData("?key1=value1&key2=value2")]
    [InlineData("key1=value1&key2=value2")]
    public void ParseQueryWithUniqueKeysWorks(string queryString)
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = queryString };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Equal(2, queryCollection.Count);
        Assert.Equal("value1", queryCollection["key1"].FirstOrDefault());
        Assert.Equal("value2", queryCollection["key2"].FirstOrDefault());
    }

    [Theory]
    [InlineData("?q", "q")]
    [InlineData("?q&", "q")]
    [InlineData("?q1=abc&q2", "q2")]
    [InlineData("?q=", "q")]
    [InlineData("?q=&", "q")]
    public void KeyWithoutValuesAddedToQueryCollection(string queryString, string emptyParam)
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = queryString };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.True(queryCollection.Keys.Contains(emptyParam));
        Assert.Equal(string.Empty, queryCollection[emptyParam]);
    }

    [Theory]
    [InlineData("?&&")]
    [InlineData("?&")]
    [InlineData("&&")]
    public void EmptyKeysNotAddedToQueryCollection(string queryString)
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = queryString };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Equal(0, queryCollection.Count);
    }

    [Fact]
    public void ParseQueryWithEmptyKeyWorks()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?=value1&=" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Single(queryCollection);
        Assert.Equal(new[] { "value1", "" }, queryCollection[""]);
    }

    [Fact]
    public void ParseQueryWithDuplicateKeysGroups()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?key1=valueA&key2=valueB&key1=valueC" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Equal(2, queryCollection.Count);
        Assert.Equal(new[] { "valueA", "valueC" }, queryCollection["key1"]);
        Assert.Equal("valueB", queryCollection["key2"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithThreefoldKeysGroups()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?key1=valueA&key2=valueB&key1=valueC&key1=valueD" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Equal(2, queryCollection.Count);
        Assert.Equal(new[] { "valueA", "valueC", "valueD" }, queryCollection["key1"]);
        Assert.Equal("valueB", queryCollection["key2"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEmptyValuesWorks()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?key1=&key2=" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Equal(2, queryCollection.Count);
        Assert.Equal(string.Empty, queryCollection["key1"].FirstOrDefault());
        Assert.Equal(string.Empty, queryCollection["key2"].FirstOrDefault());
    }

    [Theory]
    [InlineData("?")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseEmptyOrNullQueryWorks(string queryString)
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = queryString };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Empty(queryCollection);
    }

    [Fact]
    public void ParseQueryWithEncodedKeyWorks()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?fields+%5BtodoItems%5D" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Single(queryCollection);
        Assert.Equal("", queryCollection["fields [todoItems]"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEncodedValueWorks()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?=fields+%5BtodoItems%5D" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Single(queryCollection);
        Assert.Equal("fields [todoItems]", queryCollection[""].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEncodedKeyEmptyValueWorks()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?fields+%5BtodoItems%5D=" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Single(queryCollection);
        Assert.Equal("", queryCollection["fields [todoItems]"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEncodedKeyEncodedValueWorks()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?fields+%5BtodoItems%5D=%5B+1+%5D" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Single(queryCollection);
        Assert.Equal("[ 1 ]", queryCollection["fields [todoItems]"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEncodedKeyEncodedValuesWorks()
    {
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?fields+%5BtodoItems%5D=%5B+1+%5D&fields+%5BtodoItems%5D=%5B+2+%5D" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Single(queryCollection);
        Assert.Equal(new[] { "[ 1 ]", "[ 2 ]" }, queryCollection["fields [todoItems]"]);
    }

    [Fact]
    public void CaseInsensitiveWithManyKeys()
    {
        // need to use over 10 keys to test dictionary storage code path
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature
        {
            QueryString = "?a=0&b=0&c=1&d=2&e=3&f=4&g=5&h=6&i=7&j=8&k=9&" +
                "key=1&Key=2&key=3&Key=4&KEy=5&KEY=6&kEY=7&KeY=8&kEy=9&keY=10"
        };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Equal(12, queryCollection.Count);
        Assert.Equal(new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }, queryCollection["KEY"]);
    }

    [Fact]
    public void CaseInsensitiveWithFewKeys()
    {
        // need to use less than 10 keys to test array storage code path
        var features = new FeatureCollection();
        features[typeof(IHttpRequestFeature)] = new HttpRequestFeature { QueryString = "?key=1&Key=2&key=3&Key=4&KEy=5" };

        var provider = new QueryFeature(features);

        var queryCollection = provider.Query;

        Assert.Equal(1, queryCollection.Count);
        Assert.Equal(new[] { "1", "2", "3", "4", "5" }, queryCollection["KEY"]);
    }
}
