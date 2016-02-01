// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class FrameResponseHeadersTests
    {
        [Fact]
        public void InitialDictionaryContainsServerAndDate()
        {
            var connectionContext = new ConnectionContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                HttpComponentFactory = new HttpComponentFactory()
            };
            var frame = new Frame<object>(application: null, context: connectionContext)
                            .InitializeHeaders();

            IDictionary<string, StringValues> headers = frame.ResponseHeaders;

            Assert.Equal(2, headers.Count);

            StringValues serverHeader;
            Assert.True(headers.TryGetValue("Server", out serverHeader));
            Assert.Equal(1, serverHeader.Count);
            Assert.Equal("Kestrel", serverHeader[0]);

            StringValues dateHeader;
            DateTime date;
            Assert.True(headers.TryGetValue("Date", out dateHeader));
            Assert.Equal(1, dateHeader.Count);
            Assert.True(DateTime.TryParse(dateHeader[0], out date));
            Assert.True(DateTime.Now - date <= TimeSpan.FromMinutes(1));

            Assert.False(headers.IsReadOnly);
        }

        [Fact]
        public void InitialEntriesCanBeCleared()
        {
            var connectionContext = new ConnectionContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                HttpComponentFactory = new HttpComponentFactory()
            };
            var frame = new Frame<object>(application: null, context: connectionContext)
                            .InitializeHeaders();

            Assert.True(frame.ResponseHeaders.Count > 0);

            frame.ResponseHeaders.Clear();

            Assert.Equal(0, frame.ResponseHeaders.Count);
            Assert.False(frame.ResponseHeaders.ContainsKey("Server"));
            Assert.False(frame.ResponseHeaders.ContainsKey("Date"));
        }
    }
}
