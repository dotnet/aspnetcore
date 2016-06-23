// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
          new[] { "", "", "", "" }
        };

        [Fact]
        public void PropertiesAreAccessible()
        {
            var headers = new HeaderDictionary(
                new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Header1", "Value1" }
                });

            Assert.Equal(1, headers.Count);
            Assert.Equal<string>(new[] { "Header1" }, headers.Keys);
            Assert.True(headers.ContainsKey("header1"));
            Assert.False(headers.ContainsKey("header2"));
            Assert.Equal("Value1", headers["header1"]);
            Assert.Equal(new[] { "Value1" }, headers["header1"].ToArray());
        }

        [Theory]
        [MemberData(nameof(HeaderSegmentData))]
        public void EmptyHeaderSegmentsAreParsable(IEnumerable<string> segments)
        {
            var header = string.Join(",", segments);

            var headers = new HeaderDictionary(
               new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase)
               {
                    { "Header1",  header},
               });

            var result = headers.GetCommaSeparatedValues("Header1");

            Assert.Equal(segments, result);
        }
    }
}