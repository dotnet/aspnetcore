// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Http.Extensions
{
    public class QueryBuilderTests
    {
        [Fact]
        public void EmptyQuery_NoQuestionMark()
        {
            var builder = new QueryBuilder();
            Assert.Equal(string.Empty, builder.ToString());
        }

        [Fact]
        public void AddSimple_NoEncoding()
        {
            var builder = new QueryBuilder();
            builder.Add("key", "value");
            Assert.Equal("?key=value", builder.ToString());
        }

        [Fact]
        public void AddSpace_PercentEncoded()
        {
            var builder = new QueryBuilder();
            builder.Add("key", "value 1");
            Assert.Equal("?key=value%201", builder.ToString());
        }

        [Fact]
        public void AddReservedCharacters_PercentEncoded()
        {
            var builder = new QueryBuilder();
            builder.Add("key&", "value#");
            Assert.Equal("?key%26=value%23", builder.ToString());
        }

        [Fact]
        public void AddMultipleValues_AddedInOrder()
        {
            var builder = new QueryBuilder();
            builder.Add("key1", "value1");
            builder.Add("key2", "value2");
            builder.Add("key3", "value3");
            Assert.Equal("?key1=value1&key2=value2&key3=value3", builder.ToString());
        }

        [Fact]
        public void AddIEnumerableValues_AddedInOrder()
        {
            var builder = new QueryBuilder();
            builder.Add("key", new[] { "value1", "value2", "value3" });
            Assert.Equal("?key=value1&key=value2&key=value3", builder.ToString());
        }

        [Fact]
        public void AddMultipleValuesViaConstructor_AddedInOrder()
        {
            var builder = new QueryBuilder(new[]
            {
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "value2"),
                new KeyValuePair<string, string>("key3", "value3"),
            });
            Assert.Equal("?key1=value1&key2=value2&key3=value3", builder.ToString());
        }

        [Fact]
        public void AddMultipleValuesViaInitializer_AddedInOrder()
        {
            var builder = new QueryBuilder()
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
            };
            Assert.Equal("?key1=value1&key2=value2&key3=value3", builder.ToString());
        }

        [Fact]
        public void CopyViaConstructor_AddedInOrder()
        {
            var builder = new QueryBuilder()
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
            };
            var builder1 = new QueryBuilder(builder);
            Assert.Equal("?key1=value1&key2=value2&key3=value3", builder1.ToString());
        }
    }
}