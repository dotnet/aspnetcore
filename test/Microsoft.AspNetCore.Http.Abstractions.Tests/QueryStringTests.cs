// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Http.Abstractions
{
    public class QueryStringTests
    {
        [Fact]
        public void CtorThrows_IfQueryDoesNotHaveLeadingQuestionMark()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgument(() => new QueryString("hello"), "value", "The leading '?' must be included for a non-empty query.");
        }

        [Fact]
        public void CtorNullOrEmpty_Success()
        {
            var query = new QueryString();
            Assert.False(query.HasValue);
            Assert.Null(query.Value);

            query = new QueryString(null);
            Assert.False(query.HasValue);
            Assert.Null(query.Value);

            query = new QueryString(string.Empty);
            Assert.False(query.HasValue);
            Assert.Equal(string.Empty, query.Value);
        }

        [Fact]
        public void CtorJustAQuestionMark_Success()
        {
            var query = new QueryString("?");
            Assert.True(query.HasValue);
            Assert.Equal("?", query.Value);
        }

        [Fact]
        public void ToString_EncodesHash()
        {
            var query = new QueryString("?Hello=Wor#ld");
            Assert.Equal("?Hello=Wor%23ld", query.ToString());
        }

        [Theory]
        [InlineData("name", "value", "?name=value")]
        [InlineData("na me", "val ue", "?na%20me=val%20ue")]
        [InlineData("name", "", "?name=")]
        [InlineData("name", null, "?name=")]
        [InlineData("", "value", "?=value")]
        [InlineData("", "", "?=")]
        [InlineData("", null, "?=")]
        public void CreateNameValue_Success(string name, string value, string exepcted)
        {
            var query = QueryString.Create(name, value);
            Assert.Equal(exepcted, query.Value);
        }

        [Fact]
        public void CreateFromList_Success()
        {
            var query = QueryString.Create(new[]
            {
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "value2"),
                new KeyValuePair<string, string>("key3", "value3"),
                new KeyValuePair<string, string>("key4", null),
                new KeyValuePair<string, string>("key5", "")
            });
            Assert.Equal("?key1=value1&key2=value2&key3=value3&key4=&key5=", query.Value);
        }

        [Fact]
        public void CreateFromListStringValues_Success()
        {
            var query = QueryString.Create(new[]
            {
                new KeyValuePair<string, StringValues>("key1", new StringValues("value1")),
                new KeyValuePair<string, StringValues>("key2", new StringValues("value2")),
                new KeyValuePair<string, StringValues>("key3", new StringValues("value3")),
                new KeyValuePair<string, StringValues>("key4", new StringValues()),
                new KeyValuePair<string, StringValues>("key5", new StringValues("")),
            });
            Assert.Equal("?key1=value1&key2=value2&key3=value3&key4=&key5=", query.Value);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", "", "")]
        [InlineData(null, "?name2=value2", "?name2=value2")]
        [InlineData("", "?name2=value2", "?name2=value2")]
        [InlineData("?", "?name2=value2", "?name2=value2")]
        [InlineData("?name1=value1", null, "?name1=value1")]
        [InlineData("?name1=value1", "", "?name1=value1")]
        [InlineData("?name1=value1", "?", "?name1=value1")]
        [InlineData("?name1=value1", "?name2=value2", "?name1=value1&name2=value2")]
        public void AddQueryString_Success(string query1, string query2, string expected)
        {
            var q1 = new QueryString(query1);
            var q2 = new QueryString(query2);
            Assert.Equal(expected, q1.Add(q2).Value);
            Assert.Equal(expected, (q1 + q2).Value);
        }

        [Theory]
        [InlineData("", "", "", "?=")]
        [InlineData("", "", null, "?=")]
        [InlineData("?", "", "", "?=")]
        [InlineData("?", "", null, "?=")]
        [InlineData("?", "name2", "value2", "?name2=value2")]
        [InlineData("?", "name2", "", "?name2=")]
        [InlineData("?", "name2", null, "?name2=")]
        [InlineData("?name1=value1", "name2", "value2", "?name1=value1&name2=value2")]
        [InlineData("?name1=value1", "na me2", "val ue2", "?name1=value1&na%20me2=val%20ue2")]
        [InlineData("?name1=value1", "", "", "?name1=value1&=")]
        [InlineData("?name1=value1", "", null, "?name1=value1&=")]
        [InlineData("?name1=value1", "name2", "", "?name1=value1&name2=")]
        [InlineData("?name1=value1", "name2", null, "?name1=value1&name2=")]
        public void AddNameValue_Success(string query1, string name2, string value2, string expected)
        {
            var q1 = new QueryString(query1);
            var q2 = q1.Add(name2, value2);
            Assert.Equal(expected, q2.Value);
        }

        [Fact]
        public void Equals_EmptyQueryStringAndDefaultQueryString()
        {
            // Act and Assert
            Assert.Equal(default(QueryString), QueryString.Empty);
            Assert.Equal(default(QueryString), QueryString.Empty);
            // explicitly checking == operator
            Assert.True(QueryString.Empty == default(QueryString));
            Assert.True(default(QueryString) == QueryString.Empty);
        }

        [Fact]
        public void NotEquals_DefaultQueryStringAndNonNullQueryString()
        {
            // Arrange
            var queryString = new QueryString("?foo=1");

            // Act and Assert
            Assert.NotEqual(default(QueryString), queryString);
        }

        [Fact]
        public void NotEquals_EmptyQueryStringAndNonNullQueryString()
        {
            // Arrange
            var queryString = new QueryString("?foo=1");

            // Act and Assert
            Assert.NotEqual(queryString, QueryString.Empty);
        }
    }
}
