// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace AuthSamples.FunctionalTests
{
    public class DynamicSchemeTests : IClassFixture<SampleTestFixture<DynamicSchemes.Startup>>
    {
        public DynamicSchemeTests(SampleTestFixture<DynamicSchemes.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task DefaultReturns200()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // TODO: add tests verifying add works, remove works
    }
}
