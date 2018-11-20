// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class HeaderDictionaryTests
    {
        public static TheoryData HeaderSegmentData => new TheoryData<IEnumerable<string>>
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
        public void EmtpyQuotedHeaderSegmentsAreIgnored()
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
    }
}