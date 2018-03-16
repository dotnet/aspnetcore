// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    public class InvalidReadWriteOperationTests
    {
        private readonly IISTestSiteFixture _fixture;

        public InvalidReadWriteOperationTests(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task TestReadOffsetWorks()
        {
            var result = await _fixture.Client.PostAsync($"/TestReadOffsetWorks", new StringContent("Hello World"));
            Assert.Equal("Hello World", await result.Content.ReadAsStringAsync());
        }

        [ConditionalTheory]
        [InlineData("/NullBuffer")]
        [InlineData("/InvalidOffsetSmall")]
        [InlineData("/InvalidOffsetLarge")]
        [InlineData("/InvalidCountSmall")]
        [InlineData("/InvalidCountLarge")]
        [InlineData("/InvalidCountWithOffset")]
        [InlineData("/InvalidCountZeroRead")]
        public async Task TestInvalidReadOperations(string operation)
        {
            var result = await _fixture.Client.GetStringAsync($"/TestInvalidReadOperations{operation}");
            Assert.Equal("Success", result);
        }

        [ConditionalTheory]
        [InlineData("/NullBuffer")]
        [InlineData("/InvalidOffsetSmall")]
        [InlineData("/InvalidOffsetLarge")]
        [InlineData("/InvalidCountSmall")]
        [InlineData("/InvalidCountLarge")]
        [InlineData("/InvalidCountWithOffset")]
        public async Task TestInvalidWriteOperations(string operation)
        {
            var result = await _fixture.Client.GetStringAsync($"/TestInvalidWriteOperations{operation}");
            Assert.Equal("Success", result);
        }
    }
}
