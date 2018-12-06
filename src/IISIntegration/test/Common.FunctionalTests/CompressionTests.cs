// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(IISCompressionSiteCollection.Name)]
    public abstract class CompressionTests : FixtureLoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        [Collection(IISTestSiteCollection.Name)]
        public class InProc: CompressionTests
        {
            public InProc(IISTestSiteFixture fixture) : base(fixture) { }
        }

        [Collection(OutOfProcessTestSiteCollection.Name)]
        public class OutOfProcess: CompressionTests
        {
            public OutOfProcess(OutOfProcessTestSiteFixture fixture) : base(fixture) { }
        }

        [Collection(OutOfProcessV1TestSiteCollection.Name)]
        public class OutOfProcessV1: CompressionTests
        {
            public OutOfProcessV1(OutOfProcessV1TestSiteFixture fixture) : base(fixture) { }
        }

        protected CompressionTests(IISTestSiteFixture fixture) : base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task PassesThroughCompression()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/CompressedData");

            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            var response = await _fixture.Client.SendAsync(request);
            Assert.Equal("gzip", response.Content.Headers.ContentEncoding.Single());
            Assert.Equal(
                new byte[] {
                    0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x04, 0x0A, 0x63, 0x60, 0xA0, 0x3D, 0x00, 0x00,
                    0xCA, 0xC6, 0x88, 0x99, 0x64, 0x00, 0x00, 0x00 },
                await response.Content.ReadAsByteArrayAsync());
        }
    }
}
