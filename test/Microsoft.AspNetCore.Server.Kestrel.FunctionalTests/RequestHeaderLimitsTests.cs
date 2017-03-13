// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Xunit;
using Microsoft.Extensions.Primitives;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestHeaderLimitsTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(0, 1337)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(1, 1337)]
        [InlineData(5, 0)]
        [InlineData(5, 1)]
        [InlineData(5, 1337)]
        public async Task ServerAcceptsRequestWithHeaderTotalSizeWithinLimit(int headerCount, int extraLimit)
        {
            var headers = MakeHeaders(headerCount);

            using (var server = CreateServer(maxRequestHeadersTotalSize: headers.Length + extraLimit))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(0, 1337)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 1337)]
        [InlineData(5, 5)]
        [InlineData(5, 6)]
        [InlineData(5, 1337)]
        public async Task ServerAcceptsRequestWithHeaderCountWithinLimit(int headerCount, int maxHeaderCount)
        {
            var headers = MakeHeaders(headerCount);

            using (var server = CreateServer(maxRequestHeaderCount: maxHeaderCount))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 5)]
        [InlineData(100, 100)]
        [InlineData(600, 100)]
        [InlineData(700, 1)]
        [InlineData(1, 700)]
        public async Task ServerAcceptsHeadersAcrossSends(int header0Count, int header1Count)
        {
            var headers0 = MakeHeaders(header0Count);
            var headers1 = MakeHeaders(header1Count, header0Count);

            using (var server = CreateServer(maxRequestHeaderCount: header0Count + header1Count))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendAll("GET / HTTP/1.1\r\n");
                    // Wait for parsing to start
                    await WaitForCondition(TimeSpan.FromSeconds(1), () => server.Frame?.RequestHeaders != null);

                    Assert.Equal(0, server.Frame.RequestHeaders.Count);

                    await connection.SendAll(headers0);
                    // Wait for headers to be parsed
                    await WaitForCondition(TimeSpan.FromSeconds(1), () => server.Frame.RequestHeaders.Count >= header0Count);

                    Assert.Equal(header0Count, server.Frame.RequestHeaders.Count);

                    await connection.SendAll(headers1);
                    // Wait for headers to be parsed
                    await WaitForCondition(TimeSpan.FromSeconds(1), () => server.Frame.RequestHeaders.Count >= header0Count + header1Count);

                    Assert.Equal(header0Count + header1Count, server.Frame.RequestHeaders.Count);

                    await connection.SendAll("\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 5)]
        public async Task ServerKeepsSameHeaderCollectionAcrossSends(int header0Count, int header1Count)
        {
            var headers0 = MakeHeaders(header0Count);
            var headers1 = MakeHeaders(header0Count, header1Count);

            using (var server = CreateServer(maxRequestHeaderCount: header0Count + header1Count))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendAll("GET / HTTP/1.1\r\n");
                    // Wait for parsing to start
                    await WaitForCondition(TimeSpan.FromSeconds(1), () => server.Frame?.RequestHeaders != null);

                    Assert.Equal(0, server.Frame.RequestHeaders.Count);

                    var newRequestHeaders = new RequestHeadersWrapper(server.Frame.RequestHeaders);
                    server.Frame.RequestHeaders = newRequestHeaders;

                    Assert.Same(newRequestHeaders, server.Frame.RequestHeaders);

                    await connection.SendAll(headers0);
                    // Wait for headers to be parsed
                    await WaitForCondition(TimeSpan.FromSeconds(1), () => server.Frame.RequestHeaders.Count >= header0Count);

                    Assert.Same(newRequestHeaders, server.Frame.RequestHeaders);
                    Assert.Equal(header0Count, server.Frame.RequestHeaders.Count);

                    await connection.SendAll(headers1);
                    // Wait for headers to be parsed
                    await WaitForCondition(TimeSpan.FromSeconds(1), () => server.Frame.RequestHeaders.Count >= header0Count + header1Count);

                    Assert.Equal(header0Count + header1Count, server.Frame.RequestHeaders.Count);

                    Assert.Same(newRequestHeaders, server.Frame.RequestHeaders);

                    await connection.SendAll("\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task ServerRejectsRequestWithHeaderTotalSizeOverLimit(int headerCount)
        {
            var headers = MakeHeaders(headerCount);

            using (var server = CreateServer(maxRequestHeadersTotalSize: headers.Length - 1))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendAll($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 431 Request Header Fields Too Large",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(5, 1)]
        [InlineData(5, 4)]
        public async Task ServerRejectsRequestWithHeaderCountOverLimit(int headerCount, int maxHeaderCount)
        {
            var headers = MakeHeaders(headerCount);

            using (var server = CreateServer(maxRequestHeaderCount: maxHeaderCount))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendAll($"GET / HTTP/1.1\r\n{headers}\r\n");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 431 Request Header Fields Too Large",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        private static async Task WaitForCondition(TimeSpan timeout, Func<bool> condition)
        {
            const int MaxWaitLoop = 150;

            var delay = (int)Math.Ceiling(timeout.TotalMilliseconds / MaxWaitLoop);

            var waitLoop = 0;
            while (waitLoop < MaxWaitLoop && !condition())
            {
                // Wait for parsing condition to trigger
                await Task.Delay(delay);
                waitLoop++;
            }
        }

        private static string MakeHeaders(int count, int startAt = 0)
        {
            return string.Join("", Enumerable
                .Range(0, count)
                .Select(i => $"Header-{startAt + i}: value{startAt + i}\r\n"));
        }

        private TestServer CreateServer(int? maxRequestHeaderCount = null, int? maxRequestHeadersTotalSize = null)
        {
            var options = new KestrelServerOptions { AddServerHeader = false };

            if (maxRequestHeaderCount.HasValue)
            {
                options.Limits.MaxRequestHeaderCount = maxRequestHeaderCount.Value;
            }

            if (maxRequestHeadersTotalSize.HasValue)
            {
                options.Limits.MaxRequestHeadersTotalSize = maxRequestHeadersTotalSize.Value;
            }

            return new TestServer(async httpContext => await httpContext.Response.WriteAsync("hello, world"), new TestServiceContext
            {
                ServerOptions = options
            });
        }

        private class RequestHeadersWrapper : IHeaderDictionary
        {
            IHeaderDictionary _innerHeaders;

            public RequestHeadersWrapper(IHeaderDictionary headers)
            {
                _innerHeaders = headers;
            }

            public StringValues this[string key] { get => _innerHeaders[key]; set => _innerHeaders[key] = value; }
            public long? ContentLength { get => _innerHeaders.ContentLength; set => _innerHeaders.ContentLength = value; }
            public ICollection<string> Keys => _innerHeaders.Keys;
            public ICollection<StringValues> Values => _innerHeaders.Values;
            public int Count => _innerHeaders.Count;
            public bool IsReadOnly => _innerHeaders.IsReadOnly;
            public void Add(string key, StringValues value) => _innerHeaders.Add(key, value);
            public void Add(KeyValuePair<string, StringValues> item) => _innerHeaders.Add(item);
            public void Clear() => _innerHeaders.Clear();
            public bool Contains(KeyValuePair<string, StringValues> item) => _innerHeaders.Contains(item);
            public bool ContainsKey(string key) => _innerHeaders.ContainsKey(key);
            public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex) => _innerHeaders.CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => _innerHeaders.GetEnumerator();
            public bool Remove(string key) => _innerHeaders.Remove(key);
            public bool Remove(KeyValuePair<string, StringValues> item) => _innerHeaders.Remove(item);
            public bool TryGetValue(string key, out StringValues value) => _innerHeaders.TryGetValue(key, out value);
            IEnumerator IEnumerable.GetEnumerator() => _innerHeaders.GetEnumerator();
        }
    }
}