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

        //[ConditionalFact]
        //[RequiresNewHandler]
        //public async Task ResponseBodyTest_BodyCompletionNotBlockedByOnCompleted()
        //{
        //    Assert.Equal("SlowOnCompleted", await _fixture.Client.GetStringAsync($"/SlowOnCompleted"));
        //}

        //[ConditionalFact]
        //[RequiresNewHandler]
        //public async Task ResponseBodyTest_GettingHttpContextFieldsWork()
        //{
        //    Assert.Equal("SlowOnCompleted", await _fixture.Client.GetStringAsync($"/OnCompletedHttpContext"));
        //    Assert.Equal("", await _fixture.Client.GetStringAsync($"/OnCompletedHttpContext_Completed"));
        //}

        //[ConditionalFact]
        //[RequiresNewHandler]
        //public async Task ResponseBodyTest_CompleteAsyncWorks()
        //{
        //    // The app func for CompleteAsync will not finish until CompleteAsync_Completed is sent.
        //    // This verifies that the response is sent to the client with CompleteAsync
        //    var response = await _fixture.Client.GetAsync("/CompleteAsync");
        //    Assert.True(response.IsSuccessStatusCode);

        //    var response2 = await _fixture.Client.GetAsync("/CompleteAsync_Completed");
        //    Assert.True(response2.IsSuccessStatusCode);
        //}
    }
}
