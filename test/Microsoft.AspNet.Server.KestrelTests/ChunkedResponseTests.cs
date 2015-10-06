// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class ChunkedResponseTests
    {
        [Fact]
        public async Task ResponsesAreChunkedAutomatically()
        {
            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
                response.Headers.Clear();
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }))
            {
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ZeroLengthWritesAreIgnored()
        {
            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
                response.Headers.Clear();
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello "), 0, 6);
                await response.Body.WriteAsync(new byte[0], 0, 0);
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("World!"), 0, 6);
            }))
            {
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Transfer-Encoding: chunked",
                        "",
                        "6",
                        "Hello ",
                        "6",
                        "World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task EmptyResponseBodyHandledCorrectlyWithZeroLengthWrite()
        {
            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
                response.Headers.Clear();
                await response.Body.WriteAsync(new byte[0], 0, 0);
            }))
            {
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ConnectionClosedIfExeptionThrownAfterWrite()
        {
            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
                response.Headers.Clear();
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World!"), 0, 12);
                throw new Exception();
            }))
            {
                using (var connection = new TestConnection())
                {
                    // SendEnd is not called, so it isn't the client closing the connection.
                    // client closing the connection.
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "Hello World!",
                        "");
                }
            }
        }

        [Fact]
        public async Task ConnectionClosedIfExeptionThrownAfterZeroLengthWrite()
        {
            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
                response.Headers.Clear();
                await response.Body.WriteAsync(new byte[0], 0, 0);
                throw new Exception();
            }))
            {
                using (var connection = new TestConnection())
                {
                    // SendEnd is not called, so it isn't the client closing the connection.
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");

                    // Headers are sent before connection is closed, but chunked body terminator isn't sent
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Transfer-Encoding: chunked",
                        "",
                        "");
                }
            }
        }
    }
}

