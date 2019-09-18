// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
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
        [InlineData("/InvalidOffsetSmall")]
        [InlineData("/InvalidOffsetLarge")]
        [InlineData("/InvalidCountSmall")]
        [InlineData("/InvalidCountLarge")]
        [InlineData("/InvalidCountWithOffset")]
        public async Task TestInvalidReadOperations(string operation)
        {
            var result = await _fixture.Client.GetStringAsync($"/TestInvalidReadOperations{operation}");
            Assert.Equal("Success", result);
        }

        [ConditionalTheory]
        [InlineData("/NullBuffer")]
        [InlineData("/InvalidCountZeroRead")]
        public async Task TestValidReadOperations(string operation)
        {
            var result = await _fixture.Client.GetStringAsync($"/TestValidReadOperations{operation}");
            Assert.Equal("Success", result);
        }

        [ConditionalTheory]
        [InlineData("/NullBufferPost")]
        [InlineData("/InvalidCountZeroReadPost")]
        public async Task TestValidReadOperationsPost(string operation)
        {
            var result = await _fixture.Client.PostAsync($"/TestValidReadOperations{operation}", new StringContent("hello"));
            Assert.Equal("Success", await result.Content.ReadAsStringAsync());
        }

        [ConditionalTheory]
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

        [ConditionalFact]
        public async Task TestValidWriteOperations()
        {
            var result = await _fixture.Client.GetStringAsync($"/TestValidWriteOperations/NullBuffer");
            Assert.Equal("Success", result);
        }

        [ConditionalFact]
        public async Task TestValidWriteOperationsPost()
        {
            var result = await _fixture.Client.PostAsync($"/TestValidWriteOperations/NullBufferPost", new StringContent("hello"));
            Assert.Equal("Success", await result.Content.ReadAsStringAsync());
        }
    }
}
