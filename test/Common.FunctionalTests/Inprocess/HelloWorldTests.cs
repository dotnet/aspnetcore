// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    public class HelloWorldInProcessTests
    {
        private readonly IISTestSiteFixture _fixture;

        public HelloWorldInProcessTests(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task HelloWorld_InProcess()
        {
            Assert.Equal("Hello World", await _fixture.Client.GetStringAsync("/HelloWorld"));

            Assert.Equal("/Path??", await _fixture.Client.GetStringAsync("/HelloWorld/Path%3F%3F?query"));

            Assert.Equal("?query", await _fixture.Client.GetStringAsync("/HelloWorld/Query%3F%3F?query"));
        }
    }
}
