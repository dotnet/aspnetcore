// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities
{
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
        public void AddQueryStringWithNullValueThrows()
        {
            Assert.Throws<ArgumentNullException>("value" ,() => QueryHelpers.AddQueryString("http://contoso.com/", "hello", null));
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
            var queryStrings = new Dictionary<string, string>()
                        {
                            { "hello", "world" },
                            { "some", "text" },
                            { "another", string.Empty },
                            { "invisible", null }
                        };

            var result = QueryHelpers.AddQueryString(uri, queryStrings);
            Assert.Equal(expectedUri, result);
        }
    }
}
