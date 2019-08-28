// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISTestSiteCollection.Name)]
    public class LargeResponseBodyTests
    {
        private readonly IISTestSiteFixture _fixture;

        public LargeResponseBodyTests(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalTheory]
        [InlineData(65000)]
        [InlineData(1000000)]
        [InlineData(10000000)]
        [InlineData(100000000)]
        public async Task LargeResponseBodyTest_CheckAllResponseBodyBytesWritten(int query)
        {
            Assert.Equal(new string('a', query), await _fixture.Client.GetStringAsync($"/LargeResponseBody?length={query}"));
        }

        [ConditionalFact]
        public async Task LargeResponseBodyFromFile_CheckAllResponseBodyBytesWritten()
        {
            Assert.Equal(200000000, (await _fixture.Client.GetStringAsync($"/LargeResponseFile")).Length);
        }
    }
}
