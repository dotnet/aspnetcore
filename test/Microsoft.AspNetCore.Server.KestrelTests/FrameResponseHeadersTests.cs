// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class FrameResponseHeadersTests
    {
        [Fact]
        public void InitialDictionaryIsEmpty()
        {
            var serverOptions = new KestrelServerOptions();

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = serverOptions
            };
            var listenerContext = new ListenerContext(serviceContext)
            {
                ServerAddress = ServerAddress.FromUrl("http://localhost:5000")
            };
            var connectionContext = new ConnectionContext(listenerContext);

            var frame = new Frame<object>(application: null, context: connectionContext);

            frame.InitializeHeaders();

            IDictionary<string, StringValues> headers = frame.ResponseHeaders;

            Assert.Equal(0, headers.Count);
            Assert.False(headers.IsReadOnly);
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
        [InlineData("\u0000Server", "Data")]
        [InlineData("Server", "Data\u0000")]
        [InlineData("\u001FServer", "Data")]
        [InlineData("Unknown-Header\r\n", "Data")]
        [InlineData("\0Unknown-Header", "Data")]
        [InlineData("Unknown\r-Header", "Data")]
        [InlineData("Unk\nown-Header", "Data")]
        [InlineData("Server", "Da\u007Fta")]
        [InlineData("Unknown\u007F-Header", "Data")]
        [InlineData("Ser\u0080ver", "Data")]
        [InlineData("Server", "Da\u0080ta")]
        [InlineData("Unknown\u0080-Header", "Data")]
        [InlineData("Ser™ver", "Data")]
        [InlineData("Server", "Da™ta")]
        [InlineData("Unknown™-Header", "Data")]
        [InlineData("Ser™ver", "Data")]
        [InlineData("šerver", "Data")]
        [InlineData("Server", "Dašta")]
        [InlineData("Unknownš-Header", "Data")]
        [InlineData("Seršver", "Data")]
        public void AddingControlOrNonAsciiCharactersToHeadersThrows(string key, string value)
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

        [Fact]
        public void ThrowsWhenAddingHeaderAfterReadOnlyIsSet()
        {
            var headers = new FrameResponseHeaders();
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => ((IDictionary<string, StringValues>)headers).Add("my-header", new[] { "value" }));
        }

        [Fact]
        public void ThrowsWhenChangingHeaderAfterReadOnlyIsSet()
        {
            var headers = new FrameResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;
            dictionary.Add("my-header", new[] { "value" });
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => dictionary["my-header"] = "other-value");
        }

        [Fact]
        public void ThrowsWhenRemovingHeaderAfterReadOnlyIsSet()
        {
            var headers = new FrameResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;
            dictionary.Add("my-header", new[] { "value" });
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => dictionary.Remove("my-header"));
        }

        [Fact]
        public void ThrowsWhenClearingHeadersAfterReadOnlyIsSet()
        {
            var headers = new FrameResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;
            dictionary.Add("my-header", new[] { "value" });
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => dictionary.Clear());
        }
    }
}
