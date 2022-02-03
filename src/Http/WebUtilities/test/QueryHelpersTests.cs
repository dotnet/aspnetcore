// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities;

public class QueryHelperTests
{
    [Fact]
    public void ParseQueryWithUniqueKeysWorks()
    {
        var collection = QueryHelpers.ParseQuery("?key1=value1&key2=value2");
        Assert.Equal(2, collection.Count);
        Assert.Equal("value1", collection["key1"].FirstOrDefault());
        Assert.Equal("value2", collection["key2"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithoutQuestionmarkWorks()
    {
        var collection = QueryHelpers.ParseQuery("key1=value1&key2=value2");
        Assert.Equal(2, collection.Count);
        Assert.Equal("value1", collection["key1"].FirstOrDefault());
        Assert.Equal("value2", collection["key2"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithDuplicateKeysGroups()
    {
        var collection = QueryHelpers.ParseQuery("?key1=valueA&key2=valueB&key1=valueC");
        Assert.Equal(2, collection.Count);
        Assert.Equal(new[] { "valueA", "valueC" }, collection["key1"]);
        Assert.Equal("valueB", collection["key2"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEmptyValuesWorks()
    {
        var collection = QueryHelpers.ParseQuery("?key1=&key2=");
        Assert.Equal(2, collection.Count);
        Assert.Equal(string.Empty, collection["key1"].FirstOrDefault());
        Assert.Equal(string.Empty, collection["key2"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEmptyKeyWorks()
    {
        var collection = QueryHelpers.ParseQuery("?=value1&=");
        Assert.Single(collection);
        Assert.Equal(new[] { "value1", "" }, collection[""]);
    }

    [Fact]
    public void ParseQueryWithEncodedKeyWorks()
    {
        var collection = QueryHelpers.ParseQuery("?fields+%5BtodoItems%5D");
        Assert.Single(collection);
        Assert.Equal("", collection["fields [todoItems]"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEncodedValueWorks()
    {
        var collection = QueryHelpers.ParseQuery("?=fields+%5BtodoItems%5D");
        Assert.Single(collection);
        Assert.Equal("fields [todoItems]", collection[""].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEncodedKeyEmptyValueWorks()
    {
        var collection = QueryHelpers.ParseQuery("?fields+%5BtodoItems%5D=");
        Assert.Single(collection);
        Assert.Equal("", collection["fields [todoItems]"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEncodedKeyEncodedValueWorks()
    {
        var collection = QueryHelpers.ParseQuery("?fields+%5BtodoItems%5D=%5B+1+%5D");
        Assert.Single(collection);
        Assert.Equal("[ 1 ]", collection["fields [todoItems]"].FirstOrDefault());
    }

    [Fact]
    public void ParseQueryWithEncodedKeyEncodedValuesWorks()
    {
        var collection = QueryHelpers.ParseQuery("?fields+%5BtodoItems%5D=%5B+1+%5D&fields+%5BtodoItems%5D=%5B+2+%5D");
        Assert.Single(collection);
        Assert.Equal(new[] { "[ 1 ]", "[ 2 ]" }, collection["fields [todoItems]"]);
    }

    [Theory]
    [InlineData("?")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseEmptyOrNullQueryWorks(string? queryString)
    {
        var collection = QueryHelpers.ParseQuery(queryString);
        Assert.Empty(collection);
    }

    [Fact]
    public void AddQueryStringWithNullValueThrows()
    {
        Assert.Throws<ArgumentNullException>("value", () => QueryHelpers.AddQueryString("http://contoso.com/", "hello", null!));
    }

    [Theory]
    [InlineData("http://contoso.com/", "http://contoso.com/?hello=world")]
    [InlineData("http://contoso.com/someaction", "http://contoso.com/someaction?hello=world")]
    [InlineData("http://contoso.com/someaction?q=test", "http://contoso.com/someaction?q=test&hello=world")]
    [InlineData(
        "http://contoso.com/someaction?q=test#anchor",
        "http://contoso.com/someaction?q=test&hello=world#anchor")]
    [InlineData("http://contoso.com/someaction#anchor", "http://contoso.com/someaction?hello=world#anchor")]
    [InlineData("http://contoso.com/#anchor", "http://contoso.com/?hello=world#anchor")]
    [InlineData(
        "http://contoso.com/someaction?q=test#anchor?value",
        "http://contoso.com/someaction?q=test&hello=world#anchor?value")]
    [InlineData(
        "http://contoso.com/someaction#anchor?stuff",
        "http://contoso.com/someaction?hello=world#anchor?stuff")]
    [InlineData(
        "http://contoso.com/someaction?name?something",
        "http://contoso.com/someaction?name?something&hello=world")]
    [InlineData(
        "http://contoso.com/someaction#name#something",
        "http://contoso.com/someaction?hello=world#name#something")]
    public void AddQueryStringWithKeyAndValue(string uri, string expectedUri)
    {
        var result = QueryHelpers.AddQueryString(uri, "hello", "world");
        Assert.Equal(expectedUri, result);
    }

    [Theory]
    [InlineData("http://contoso.com/", "http://contoso.com/?hello=world&some=text&another=")]
    [InlineData("http://contoso.com/someaction", "http://contoso.com/someaction?hello=world&some=text&another=")]
    [InlineData("http://contoso.com/someaction?q=1", "http://contoso.com/someaction?q=1&hello=world&some=text&another=")]
    [InlineData("http://contoso.com/some#action", "http://contoso.com/some?hello=world&some=text&another=#action")]
    [InlineData("http://contoso.com/some?q=1#action", "http://contoso.com/some?q=1&hello=world&some=text&another=#action")]
    [InlineData("http://contoso.com/#action", "http://contoso.com/?hello=world&some=text&another=#action")]
    [InlineData(
        "http://contoso.com/someaction?q=test#anchor?value",
        "http://contoso.com/someaction?q=test&hello=world&some=text&another=#anchor?value")]
    [InlineData(
        "http://contoso.com/someaction#anchor?stuff",
        "http://contoso.com/someaction?hello=world&some=text&another=#anchor?stuff")]
    [InlineData(
        "http://contoso.com/someaction?name?something",
        "http://contoso.com/someaction?name?something&hello=world&some=text&another=")]
    [InlineData(
        "http://contoso.com/someaction#name#something",
        "http://contoso.com/someaction?hello=world&some=text&another=#name#something")]
    public void AddQueryStringWithDictionary(string uri, string expectedUri)
    {
        var queryStrings = new Dictionary<string, string?>()
                        {
                            { "hello", "world" },
                            { "some", "text" },
                            { "another", string.Empty },
                            { "invisible", null }
                        };

        var result = QueryHelpers.AddQueryString(uri, queryStrings);
        Assert.Equal(expectedUri, result);
    }

    [Theory]
    [InlineData("http://contoso.com/", "http://contoso.com/?param1=value1&param1=&param1=value3&param2=")]
    [InlineData("http://contoso.com/someaction", "http://contoso.com/someaction?param1=value1&param1=&param1=value3&param2=")]
    [InlineData("http://contoso.com/someaction?param2=1", "http://contoso.com/someaction?param2=1&param1=value1&param1=&param1=value3&param2=")]
    [InlineData("http://contoso.com/some#action", "http://contoso.com/some?param1=value1&param1=&param1=value3&param2=#action")]
    [InlineData("http://contoso.com/some?param2=1#action", "http://contoso.com/some?param2=1&param1=value1&param1=&param1=value3&param2=#action")]
    [InlineData("http://contoso.com/#action", "http://contoso.com/?param1=value1&param1=&param1=value3&param2=#action")]
    [InlineData(
        "http://contoso.com/someaction?q=test#anchor?value",
        "http://contoso.com/someaction?q=test&param1=value1&param1=&param1=value3&param2=#anchor?value")]
    [InlineData(
        "http://contoso.com/someaction#anchor?stuff",
        "http://contoso.com/someaction?param1=value1&param1=&param1=value3&param2=#anchor?stuff")]
    [InlineData(
        "http://contoso.com/someaction?name?something",
        "http://contoso.com/someaction?name?something&param1=value1&param1=&param1=value3&param2=")]
    [InlineData(
        "http://contoso.com/someaction#name#something",
        "http://contoso.com/someaction?param1=value1&param1=&param1=value3&param2=#name#something")]
    public void AddQueryStringWithEnumerableOfKeysAndStringValues(string uri, string expectedUri)
    {
        var queryStrings = new Dictionary<string, StringValues>()
                        {
                            { "param1", new StringValues(new [] { "value1", string.Empty, "value3" }) },
                            { "param2", string.Empty },
                            { "param3", StringValues.Empty }
                        };

        var result = QueryHelpers.AddQueryString(uri, queryStrings);
        Assert.Equal(expectedUri, result);
    }
}
