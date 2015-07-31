// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Server.Kestrel.Http;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class FrameResponseHeadersTests
    {
        [Fact]
        public void InitialDictionaryContainsServerAndDate()
        {
            IDictionary<string, string[]> headers = new FrameResponseHeaders();

            Assert.Equal(2, headers.Count);

            string[] serverHeader;
            Assert.True(headers.TryGetValue("Server", out serverHeader));
            Assert.Equal(1, serverHeader.Length);
            Assert.Equal("Kestrel", serverHeader[0]);

            string[] dateHeader;
            DateTime date;
            Assert.True(headers.TryGetValue("Date", out dateHeader));
            Assert.Equal(1, dateHeader.Length);
            Assert.True(DateTime.TryParse(dateHeader[0], out date));
            Assert.True(DateTime.Now - date <= TimeSpan.FromMinutes(1));

            Assert.False(headers.IsReadOnly);
        }

        [Fact]
        public void InitialEntriesCanBeCleared()
        {
            IDictionary<string, string[]> headers = new FrameResponseHeaders();

            headers.Clear();

            Assert.Equal(0, headers.Count);
            Assert.False(headers.ContainsKey("Server"));
            Assert.False(headers.ContainsKey("Date"));
        }
    }
}
