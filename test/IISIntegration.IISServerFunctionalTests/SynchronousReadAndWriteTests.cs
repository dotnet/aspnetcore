// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{ 
    [Collection(IISTestSiteCollection.Name)]
    public class SynchronousReadAndWriteTests
    {
        private readonly IISTestSiteFixture _fixture;

        public SynchronousReadAndWriteTests(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task ReadAndWriteSynchronously()
        {
            for (int i = 0; i < 100; i++)
            {
                var content = new StringContent(new string('a', 100000));
                var response = await _fixture.Client.PostAsync("ReadAndWriteSynchronously", content);
                var responseText = await response.Content.ReadAsStringAsync();

                Assert.Equal(expected: 110000, actual: responseText.Length);
            }
        }

        [ConditionalFact]
        public async Task ReadAndWriteEcho()
        {
            var body = new string('a', 100000);
            var content = new StringContent(body);
            var response = await _fixture.Client.PostAsync("ReadAndWriteEcho", content);
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(body, responseText);
        }

        [ConditionalFact]
        public async Task ReadAndWriteCopyToAsync()
        {
            var body = new string('a', 100000);
            var content = new StringContent(body);
            var response = await _fixture.Client.PostAsync("ReadAndWriteCopyToAsync", content);
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(body, responseText);
        }

        [ConditionalFact]
        public async Task ReadAndWriteEchoTwice()
        {
            var requestBody = new string('a', 10000);
            var content = new StringContent(requestBody);
            var response = await _fixture.Client.PostAsync("ReadAndWriteEchoTwice", content);
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(requestBody.Length * 2, responseText.Length);
        }

        [ConditionalFact]
        public void ReadAndWriteSlowConnection()
        {
            var ipHostEntry = Dns.GetHostEntry(_fixture.Client.BaseAddress.Host);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                foreach (var hostEntry in ipHostEntry.AddressList)
                {
                    try
                    {
                        socket.Connect(hostEntry, _fixture.Client.BaseAddress.Port);
                        break;
                    }
                    catch (Exception)
                    {
                        // Exceptions can be thrown based on ipv6 support
                    }
                }

                var testString = "hello world";
                var request = $"POST /ReadAndWriteSlowConnection HTTP/1.0\r\n" +
                    $"Content-Length: {testString.Length}\r\n" +
                    "Host: " + "localhost\r\n" +
                    "\r\n";
                var bytes = 0;
                var requestStringBytes = Encoding.ASCII.GetBytes(request);
                var testStringBytes = Encoding.ASCII.GetBytes(testString);

                while ((bytes += socket.Send(requestStringBytes, bytes, 1, SocketFlags.None)) < requestStringBytes.Length)
                {
                }

                bytes = 0;
                while ((bytes += socket.Send(testStringBytes, bytes, 1, SocketFlags.None)) < testStringBytes.Length)
                {
                    Thread.Sleep(100);
                }

                var stringBuilder = new StringBuilder();
                var buffer = new byte[4096];
                int size;
                while ((size = socket.Receive(buffer, buffer.Length, SocketFlags.None)) != 0)
                {
                    stringBuilder.Append(Encoding.ASCII.GetString(buffer, 0, size));
                }

                Assert.Contains(new StringBuilder().Insert(0, "hello world", 100).ToString(), stringBuilder.ToString());
            }
        }
    }
}
