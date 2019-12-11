// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISTestSiteCollection.Name)]
    public class ResponseBodyTests
    {
        private readonly IISTestSiteFixture _fixture;

        public ResponseBodyTests(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task ResponseBodyTest_UnflushedPipe_AutoFlushed()
        {
            Assert.Equal(10, (await _fixture.Client.GetByteArrayAsync($"/UnflushedResponsePipe")).Length);
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task ResponseBodyTest_FlushedPipeAndThenUnflushedPipe_AutoFlushed()
        {
            Assert.Equal(20, (await _fixture.Client.GetByteArrayAsync($"/FlushedPipeAndThenUnflushedPipe")).Length);
        }

        [ConditionalFact]
        [RequiresNewHandler]
        public async Task ResponseBodyTest_BodyCompletionNotBlockedByOnCompleted()
        {
            Assert.Equal("SlowOnCompleted", await _fixture.Client.GetStringAsync($"/SlowOnCompleted"));
        }
    }
}
