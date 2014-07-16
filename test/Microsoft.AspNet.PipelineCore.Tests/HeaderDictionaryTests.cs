// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.PipelineCore.Collections;
using Xunit;

namespace Microsoft.AspNet.PipelineCore.Tests
{
    public class HeaderDictionaryTests
    {
        [Fact]
        public void PropertiesAreAccessible()
        {
            var headers = new HeaderDictionary(
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Header1", new[] { "Value1" } }
                });

            Assert.Equal(1, headers.Count);
            Assert.Equal(new[] { "Header1" }, headers.Keys);
            Assert.True(headers.ContainsKey("header1"));
            Assert.False(headers.ContainsKey("header2"));
            Assert.Equal("Value1", headers["header1"]);
            Assert.Equal("Value1", headers.Get("header1"));
            Assert.Equal(new[] { "Value1" }, headers.GetValues("header1"));
        }
    }
}