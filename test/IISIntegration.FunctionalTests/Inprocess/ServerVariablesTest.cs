// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    public class ServerVariablesTest
    {
        private readonly IISTestSiteFixture _fixture;

        public ServerVariablesTest(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task ProvidesAccessToServerVariables()
        {
            var port = _fixture.Client.BaseAddress.Port;
            Assert.Equal("SERVER_PORT: " + port, await _fixture.Client.GetStringAsync("/ServerVariable?q=SERVER_PORT"));
            Assert.Equal("QUERY_STRING: q=QUERY_STRING", await _fixture.Client.GetStringAsync("/ServerVariable?q=QUERY_STRING"));
        }

        [ConditionalFact]
        public async Task ReturnsNullForUndefinedServerVariable()
        {
            var port = _fixture.Client.BaseAddress.Port;
            Assert.Equal("THIS_VAR_IS_UNDEFINED: (null)", await _fixture.Client.GetStringAsync("/ServerVariable?q=THIS_VAR_IS_UNDEFINED"));
        }
    }
}
