// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class OpaqueUpgradeTests
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public async Task OpaqueUpgrade_AfterHeadersSent_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> clientTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                byte[] body = Encoding.UTF8.GetBytes("Hello World");
                await context.Response.Body.WriteAsync(body, 0, body.Length);

                Assert.Throws<InvalidOperationException>(() => context.Response.Headers["Upgrade"] = "WebSocket"); // Win8.1 blocks anything but WebSocket
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await context.UpgradeAsync());
                context.Dispose();
                HttpResponseMessage response = await clientTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public async Task OpaqueUpgrade_GetUpgrade_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<Stream> clientTask = SendOpaqueRequestAsync("GET", address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.True(context.IsUpgradableRequest);
                context.Response.Headers["Upgrade"] = "WebSocket"; // Win8.1 blocks anything but WebSocket
                Stream serverStream = await context.UpgradeAsync();
                Assert.True(serverStream.CanRead);
                Assert.True(serverStream.CanWrite);
                Stream clientStream = await clientTask;
                serverStream.Dispose();
                context.Dispose();
                clientStream.Dispose();
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        // See HTTP_VERB for known verbs
        [InlineData("UNKNOWN", null)]
        [InlineData("INVALID", null)]
        [InlineData("OPTIONS", null)]
        [InlineData("GET", null)]
        [InlineData("HEAD", null)]
        [InlineData("DELETE", null)]
        [InlineData("TRACE", null)]
        [InlineData("CONNECT", null)]
        [InlineData("TRACK", null)]
        [InlineData("MOVE", null)]
        [InlineData("COPY", null)]
        [InlineData("PROPFIND", null)]
        [InlineData("PROPPATCH", null)]
        [InlineData("MKCOL", null)]
        [InlineData("LOCK", null)]
        [InlineData("UNLOCK", null)]
        [InlineData("SEARCH", null)]
        [InlineData("CUSTOMVERB", null)]
        [InlineData("PATCH", null)]
        [InlineData("POST", "Content-Length: 0")]
        [InlineData("PUT", "Content-Length: 0")]
        public async Task OpaqueUpgrade_VariousMethodsUpgradeSendAndReceive_Success(string method, string extraHeader)
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<Stream> clientTask = SendOpaqueRequestAsync(method, address, extraHeader);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.True(context.IsUpgradableRequest);
                context.Response.Headers["Upgrade"] = "WebSocket"; // Win8.1 blocks anything but WebSocket
                Stream serverStream = await context.UpgradeAsync();
                Stream clientStream = await clientTask;

                byte[] clientBuffer = new byte[] { 0x00, 0x01, 0xFF, 0x00, 0x00 };
                await clientStream.WriteAsync(clientBuffer, 0, 3);

                byte[] serverBuffer = new byte[clientBuffer.Length];
                int read = await serverStream.ReadAsync(serverBuffer, 0, serverBuffer.Length);
                Assert.Equal(clientBuffer, serverBuffer);

                await serverStream.WriteAsync(serverBuffer, 0, read);

                byte[] clientEchoBuffer = new byte[clientBuffer.Length];
                read = await clientStream.ReadAsync(clientEchoBuffer, 0, clientEchoBuffer.Length);
                Assert.Equal(clientBuffer, clientEchoBuffer);

                serverStream.Dispose();
                context.Dispose();
                clientStream.Dispose();
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        // Http.Sys returns a 411 Length Required if PUT or POST does not specify content-length or chunked.
        [InlineData("POST", "Content-Length: 10")]
        [InlineData("POST", "Transfer-Encoding: chunked")]
        [InlineData("PUT", "Content-Length: 10")]
        [InlineData("PUT", "Transfer-Encoding: chunked")]
        [InlineData("CUSTOMVERB", "Content-Length: 10")]
        [InlineData("CUSTOMVERB", "Transfer-Encoding: chunked")]
        public async Task OpaqueUpgrade_InvalidMethodUpgrade_Disconnected(string method, string extraHeader)
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var clientTask = SendOpaqueRequestAsync(method, address, extraHeader);
                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.False(context.IsUpgradableRequest);
                context.Dispose();

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await clientTask);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }

        // Returns a bidirectional opaque stream or throws if the upgrade fails
        private async Task<Stream> SendOpaqueRequestAsync(string method, string address, string extraHeader = null)
        {
            // Connect with a socket
            Uri uri = new Uri(address);
            TcpClient client = new TcpClient();
            try
            {
                await client.ConnectAsync(uri.Host, uri.Port);
                NetworkStream stream = client.GetStream();

                // Send an HTTP GET request
                byte[] requestBytes = BuildGetRequest(method, uri, extraHeader);
                await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                // Read the response headers, fail if it's not a 101
                await ParseResponseAsync(stream);

                // Return the opaque network stream
                return stream;
            }
            catch (Exception)
            {
                ((IDisposable)client).Dispose();
                throw;
            }
        }

        private byte[] BuildGetRequest(string method, Uri uri, string extraHeader)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(method);
            builder.Append(" ");
            builder.Append(uri.PathAndQuery);
            builder.Append(" HTTP/1.1");
            builder.AppendLine();

            builder.Append("Host: ");
            builder.Append(uri.Host);
            builder.Append(':');
            builder.Append(uri.Port);
            builder.AppendLine();

            if (!string.IsNullOrEmpty(extraHeader))
            {
                builder.AppendLine(extraHeader);
            }

            builder.AppendLine();
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        // Read the response headers, fail if it's not a 101
        private async Task ParseResponseAsync(NetworkStream stream)
        {
            StreamReader reader = new StreamReader(stream);
            string statusLine = await reader.ReadLineAsync();
            string[] parts = statusLine.Split(' ');
            if (int.Parse(parts[1]) != 101)
            {
                throw new InvalidOperationException("The response status code was incorrect: " + statusLine);
            }

            // Scan to the end of the headers
            while (!string.IsNullOrEmpty(reader.ReadLine()))
            {
            }
        }
    }
}