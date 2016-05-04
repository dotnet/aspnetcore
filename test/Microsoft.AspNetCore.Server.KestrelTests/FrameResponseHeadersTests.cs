// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class FrameResponseHeadersTests
    {
        [Fact]
        public void InitialDictionaryContainsServerAndDate()
        {
            var serverOptions = new KestrelServerOptions();
            var connectionContext = new ConnectionContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = serverOptions,
                HttpComponentFactory = new HttpComponentFactory(serverOptions)
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.InitializeHeaders();

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
            var serverOptions = new KestrelServerOptions();
            var connectionContext = new ConnectionContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000"),
                ServerOptions = serverOptions,
                HttpComponentFactory = new HttpComponentFactory(serverOptions)
            };
            var frame = new Frame<object>(application: null, context: connectionContext);
            frame.InitializeHeaders();

            Assert.True(frame.ResponseHeaders.Count > 0);

            frame.ResponseHeaders.Clear();

            Assert.Equal(0, frame.ResponseHeaders.Count);
            Assert.False(frame.ResponseHeaders.ContainsKey("Server"));
            Assert.False(frame.ResponseHeaders.ContainsKey("Date"));
        }

        [Theory]
        [InlineData("Server", "\r\nData")]
        [InlineData("Server", "\0Data")]
        [InlineData("Server", "Data\r")]
        [InlineData("Server", "Da\0ta")]
        [InlineData("Server", "Da\u001Fta")]
        [InlineData("Unknown-Header", "\r\nData")]
        [InlineData("Unknown-Header", "\0Data")]
        [InlineData("Unknown-Header", "Data\0")]
        [InlineData("Unknown-Header", "Da\nta")]
        [InlineData("\r\nServer", "Data")]
        [InlineData("Server\r", "Data")]
        [InlineData("Ser\0ver", "Data")]
        [InlineData("Server\r\n", "Data")]
        [InlineData("\u001FServer", "Data")]
        [InlineData("Unknown-Header\r\n", "Data")]
        [InlineData("\0Unknown-Header", "Data")]
        [InlineData("Unknown\r-Header", "Data")]
        [InlineData("Unk\nown-Header", "Data")]
        public void AddingControlCharactersToHeadersThrows(string key, string value)
        {
            var responseHeaders = new FrameResponseHeaders();

            Assert.Throws<InvalidOperationException>(() => {
                ((IHeaderDictionary)responseHeaders)[key] = value;
            });

            Assert.Throws<InvalidOperationException>(() => {
                ((IHeaderDictionary)responseHeaders)[key] = new StringValues(new[] { "valid", value });
            });

            Assert.Throws<InvalidOperationException>(() => {
                ((IDictionary<string, StringValues>)responseHeaders)[key] = value;
            });

            Assert.Throws<InvalidOperationException>(() => {
                var kvp = new KeyValuePair<string, StringValues>(key, value);
                ((ICollection<KeyValuePair<string, StringValues>>)responseHeaders).Add(kvp);
            });

            Assert.Throws<InvalidOperationException>(() => {
                var kvp = new KeyValuePair<string, StringValues>(key, value);
                ((IDictionary<string, StringValues>)responseHeaders).Add(key, value);
            });
        }
    }
}
