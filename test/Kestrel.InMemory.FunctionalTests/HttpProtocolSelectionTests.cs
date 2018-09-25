// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class HttpProtocolSelectionTests : TestApplicationErrorLoggerLoggedTest
    {
        [Fact]
        public Task Server_NoProtocols_Error()
        {
            return TestError<InvalidOperationException>(HttpProtocols.None, CoreStrings.EndPointRequiresAtLeastOneProtocol);
        }

        [Fact]
        public Task Server_Http1AndHttp2_Cleartext_Http1Default()
        {
            return TestSuccess(HttpProtocols.Http1AndHttp2, "GET / HTTP/1.1\r\nHost:\r\n\r\n", "HTTP/1.1 200 OK");
        }

        [Fact]
        public Task Server_Http1Only_Cleartext_Success()
        {
            return TestSuccess(HttpProtocols.Http1, "GET / HTTP/1.1\r\nHost:\r\n\r\n", "HTTP/1.1 200 OK");
        }

        [Fact]
        public async Task Server_Http2Only_Cleartext_Success()
        {
            // Expect a SETTINGS frame (type 0x4) with default settings
            var expected = new byte[]
            {
                0x00, 0x00, 0x12, // Payload Length (6 * settings count)
                0x04, 0x00, 0x00, 0x00, 0x00, 0x00, // SETTINGS frame (type 0x04)
                0x00, 0x03, 0x00, 0x00, 0x00, 0x64, // Connection limit
                0x00, 0x04, 0x00, 0x01, 0x80, 0x00, // Initial window size
                0x00, 0x06, 0x00, 0x00, 0x80, 0x00 // Header size limit
            };
            var testContext = new TestServiceContext(LoggerFactory);
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                Protocols = HttpProtocols.Http2
            };

            using (var server = new TestServer(context => Task.CompletedTask, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(Encoding.ASCII.GetString(Http2Connection.ClientPreface));
                    // Can't use Receive when expecting binary data
                    var actual = new byte[expected.Length];
                    var read = await connection.Stream.ReadAsync(actual, 0, actual.Length);
                    Assert.Equal(expected.Length, read);
                    Assert.Equal(expected, actual);
                }
            }
        }

        private async Task TestSuccess(HttpProtocols serverProtocols, string request, string expectedResponse)
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                Protocols = serverProtocols
            };

            using (var server = new TestServer(context => Task.CompletedTask, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(request);
                    await connection.Receive(expectedResponse);
                }
            }
        }

        private async Task TestError<TException>(HttpProtocols serverProtocols, string expectedErrorMessage)
            where TException : Exception
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                Protocols = serverProtocols
            };

            using (var server = new TestServer(context => Task.CompletedTask, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.WaitForConnectionClose();
                }
            }

            Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error
                && message.EventId.Id == 0
                && message.Message == expectedErrorMessage);
        }
    }
}
