// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISTestSiteCollection.Name)]
    public class SynchronousReadAndWriteTests: FixtureLoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        public SynchronousReadAndWriteTests(IISTestSiteFixture fixture): base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/7341")]
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
        public async Task ReadSetHeaderWrite()
        {
            var body = "Body text";
            var content = new StringContent(body);
            var response = await _fixture.Client.PostAsync("SetHeaderFromBody", content);
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(body, response.Headers.GetValues("BodyAsString").Single());
            Assert.Equal(body, responseText);
        }

        [ConditionalFact]
        public async Task ReadAndWriteSlowConnection()
        {
            using (var connection = _fixture.CreateTestConnection())
            {
                var testString = "hello world";
                var request = $"POST /ReadAndWriteSlowConnection HTTP/1.0\r\n" +
                    $"Content-Length: {testString.Length}\r\n" +
                    "Host: " + "localhost\r\n" +
                    "\r\n" + testString;

                foreach (var c in request)
                {
                    await connection.Send(c.ToString());
                    await Task.Delay(10);
                }

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "");
                await connection.ReceiveHeaders();

                for (int i = 0; i < 100; i++)
                {
                    foreach (var c in testString)
                    {
                        await connection.Receive(c.ToString());
                    }
                    await Task.Delay(10);
                }
                await connection.WaitForConnectionClose();
            }
        }

        [ConditionalFact]
        public async Task ReadAndWriteInterleaved()
        {
            using (var connection = _fixture.CreateTestConnection())
            {
                var requestLength = 0;
                var messages = new List<string>();
                for (var i = 1; i < 100; i++)
                {
                    var message = i + Environment.NewLine;
                    requestLength += message.Length;
                    messages.Add(message);
                }

                await connection.Send(
                    "POST /ReadAndWriteEchoLines HTTP/1.0",
                    $"Content-Length: {requestLength}",
                    "Host: localhost",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "");
                await connection.ReceiveHeaders();

                foreach (var message in messages)
                {
                    await connection.Send(message);
                    await connection.Receive(message);
                }

                await connection.Send("\r\n");
                await connection.WaitForConnectionClose();
            }
        }

        [ConditionalFact]
        public async Task ConsumePartialBody()
        {
            using (var connection = _fixture.CreateTestConnection())
            {
                var message = "Hello";
                await connection.Send(
                    "POST /ReadPartialBody HTTP/1.1",
                    $"Content-Length: {100}",
                    "Host: localhost",
                    "Connection: close",
                    "",
                    "");

                await connection.Send(message);

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "");

                // This test can return both content length or chunked response
                // depending on if appfunc managed to complete before write was
                // issued
                var headers = await connection.ReceiveHeaders();
                if (headers.Contains("Content-Length: 5"))
                {
                    await connection.Receive("Hello");
                }
                else
                {
                    await connection.Receive(
                        "5",
                        message,
                        "");
                    await connection.Receive(
                        "0",
                        "",
                        "");
                }

                await connection.WaitForConnectionClose();
            }
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task AsyncChunkedPostIsAccepted()
        {
            // This test sends a lot of request because we are trying to force
            // different async completion modes from IIS
            for (int i = 0; i < 100; i++)
            {
                using (var connection = _fixture.CreateTestConnection())
                {
                    await connection.Send(
                        "POST /ReadFullBody HTTP/1.1",
                        $"Transfer-Encoding: chunked",
                        "Host: localhost",
                        "Connection: close",
                        "",
                        "");

                    await connection.Send("5",
                        "Hello",
                        "");

                    await connection.Send(
                        "0",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "");

                    await connection.ReceiveHeaders();
                    await connection.Receive("Completed");

                    await connection.WaitForConnectionClose();
                }
            }
        }
    }
}
