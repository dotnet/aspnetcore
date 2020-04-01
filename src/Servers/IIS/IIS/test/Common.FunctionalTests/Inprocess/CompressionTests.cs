// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISCompressionSiteCollection.Name)]
    public class CompressionModuleTests : FixtureLoggedTest
    {
        private readonly IISCompressionSiteFixture _fixture;

        public CompressionModuleTests(IISCompressionSiteFixture fixture): base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalTheory]
        [RequiresIIS(IISCapability.DynamicCompression)]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BufferingDisabled(bool compression)
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
                    "POST /ReadAndWriteEchoLinesNoBuffering HTTP/1.1",
                    $"Content-Length: {requestLength}",
                    "Accept-Encoding: " + (compression ? "gzip": "identity"),
                    "Response-Content-Type: text/event-stream",
                    "Host: localhost",
                    "Connection: close",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "");
                await connection.ReceiveHeaders();

                foreach (var message in messages)
                {
                    await connection.Send(message);
                    await connection.ReceiveChunk(message);
                }

                await connection.Send("\r\n");
                await connection.ReceiveChunk("");
                await connection.WaitForConnectionClose();
            }
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.DynamicCompression)]
        public async Task DynamicResponsesAreCompressed()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
            };
            var client = new HttpClient(handler)
            {
                BaseAddress = _fixture.Client.BaseAddress,
            };
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity", 0));
            client.DefaultRequestHeaders.Add("Response-Content-Type", "text/event-stream");
            var messages = "Message1\r\nMessage2\r\n\r\n";

            // Send messages with terminator
            var response = await client.PostAsync("ReadAndWriteEchoLines", new StringContent(messages));
            Assert.Equal(messages, await response.Content.ReadAsStringAsync());
            Assert.True(response.Content.Headers.TryGetValues("Content-Type", out var contentTypes));
            Assert.Single(contentTypes, "text/event-stream");
            // Not the cleanest check but I wasn't able to figure out other way to check
            // that response was compressed
            Assert.Contains("gzip", response.Content.GetType().FullName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
