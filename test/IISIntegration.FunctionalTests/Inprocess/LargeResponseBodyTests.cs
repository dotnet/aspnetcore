// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
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
        [InlineData(1000000)]
        [InlineData(10000000)]
        public async Task LargeResponseBodyTestCheckAllResponseBodyBytesWritten(int query)
        {
            Assert.Equal(new string('a', query), await _fixture.Client.GetStringAsync($"/LargeResponseBody?length={query}"));
        }
    }
}
