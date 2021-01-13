// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISTestSiteCollection.Name)]
    public class ClientDisconnectTests: FixtureLoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        public ClientDisconnectTests(IISTestSiteFixture fixture): base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task ServerWorksAfterClientDisconnect()
        {
            using (var connection = _fixture.CreateTestConnection())
            {
                var message = "Hello";
                await connection.Send(
                    "POST /ReadAndWriteSynchronously HTTP/1.1",
                    $"Content-Length: {100000}",
                    "Host: localhost",
                    "Connection: close",
                    "",
                    "");

                await connection.Send(message);

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "");
            }

            var response = await _fixture.Client.GetAsync("HelloWorld");

            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World", responseText);
        }

        [ConditionalFact]
        public async Task RequestAbortedTokenFires()
        {
            using (var connection = _fixture.CreateTestConnection())
            {
                await connection.Send(
                    "GET /WaitForAbort HTTP/1.1",
                    "Host: localhost",
                    "Connection: close",
                    "",
                    "");

                await _fixture.Client.RetryRequestAsync("/WaitingRequestCount", async message => await message.Content.ReadAsStringAsync() == "1");
            }

            await _fixture.Client.RetryRequestAsync("/WaitingRequestCount", async message => await message.Content.ReadAsStringAsync() == "0");
        }

        [ConditionalFact]
        public async Task ClientDisconnectCallbackStress()
        {
            // Fixture initialization fails if inside of the Task.Run, so send an
            // initial request to initialize the fixture.
            var response = await _fixture.Client.GetAsync("HelloWorld");
            var numTotalRequests = 0;
            for (var j = 0; j < 20; j++)
            {
                // Windows has a max connection limit of 10 for the IIS server,
                // so setting limit fairly low.
                const int numRequests = 5;
                async Task RunRequests()
                {
                    using (var connection = _fixture.CreateTestConnection())
                    {
                        await connection.Send(
                            "GET /WaitForAbort HTTP/1.1",
                            "Host: localhost",
                            "Connection: close",
                            "",
                            "");
                        await _fixture.Client.RetryRequestAsync("/WaitingRequestCount", async message => await message.Content.ReadAsStringAsync() != "0");
                        Interlocked.Increment(ref numTotalRequests);
                    }
                }

                List<Task> tasks = new List<Task>();
                for (int i = 0; i < numRequests; i++)
                {
                    tasks.Add(Task.Run(RunRequests));
                }

                await Task.WhenAll(tasks);

                await _fixture.Client.RetryRequestAsync("/WaitingRequestCount", async message => await message.Content.ReadAsStringAsync() == "0");
            }
        }
    }
}
