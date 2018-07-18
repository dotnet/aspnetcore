// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
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
    }
}
