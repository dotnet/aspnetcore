// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class HttpProtocolSelectionTests
    {
        [Fact]
        public Task Server_NoProtocols_Error()
        {
            return TestError<InvalidOperationException>(HttpProtocols.None, CoreStrings.EndPointRequiresAtLeastOneProtocol);
        }

        [Fact]
        public Task Server_Http1AndHttp2_Cleartext_Error()
        {
            return TestError<InvalidOperationException>(HttpProtocols.Http1AndHttp2, CoreStrings.EndPointRequiresTlsForHttp1AndHttp2);
        }

        [Fact]
        public Task Server_Http1Only_Cleartext_Success()
        {
            return TestSuccess(HttpProtocols.Http1, "GET / HTTP/1.1\r\nHost:\r\n\r\n", "HTTP/1.1 200 OK");
        }

        [Fact]
        public Task Server_Http2Only_Cleartext_Success()
        {
            // Expect a SETTINGS frame (type 0x4) with no payload and no flags
            return TestSuccess(HttpProtocols.Http2, Encoding.ASCII.GetString(Http2Connection.ClientPreface), "\x00\x00\x00\x04\x00\x00\x00\x00\x00");
        }

        private async Task TestSuccess(HttpProtocols serverProtocols, string request, string expectedResponse)
        {
            var builder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 0, listenOptions =>
                    {
                        listenOptions.Protocols = serverProtocols;
                    });
                })
                .Configure(app => app.Run(context => Task.CompletedTask));

            using (var host = builder.Build())
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.Send(request);
                    await connection.Receive(expectedResponse);
                }
            }
        }

        private async Task TestError<TException>(HttpProtocols serverProtocols, string expectedErrorMessage)
            where TException : Exception
        {
            var logger = new TestApplicationErrorLogger();
            var loggerProvider = new Mock<ILoggerProvider>();
            loggerProvider
                .Setup(provider => provider.CreateLogger(It.IsAny<string>()))
                .Returns(logger);

            var builder = TransportSelector.GetWebHostBuilder()
                .ConfigureLogging(loggingBuilder => loggingBuilder.AddProvider(loggerProvider.Object))
                .UseKestrel(options => options.Listen(IPAddress.Loopback, 0, listenOptions =>
                {
                    listenOptions.Protocols = serverProtocols;
                }))
                .Configure(app => app.Run(context => Task.CompletedTask));

            using (var host = builder.Build())
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.WaitForConnectionClose().TimeoutAfter(TestConstants.DefaultTimeout);
                }
            }

            Assert.Single(logger.Messages, message => message.LogLevel == LogLevel.Error
                && message.EventId.Id == 0
                && message.Message == expectedErrorMessage);
        }
    }
}
