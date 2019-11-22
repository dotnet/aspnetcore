// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
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
            Assert.Equal("THIS_VAR_IS_UNDEFINED: (null)", await _fixture.Client.GetStringAsync("/ServerVariable?q=THIS_VAR_IS_UNDEFINED"));
        }

        [ConditionalFact]
        public async Task CanSetAndReadVariable()
        {
            Assert.Equal("ROUNDTRIP: 1", await _fixture.Client.GetStringAsync("/ServerVariable?v=1&q=ROUNDTRIP"));
        }

        [ConditionalFact]
        public async Task BasePathIsNotPrefixedBySlashSlashQuestionMark()
        {
            Assert.DoesNotContain(@"\\?\", await _fixture.Client.GetStringAsync("/BasePath"));
        }

        [ConditionalFact]
        public async Task GetServerVariableDoesNotCrash()
        {
            await Helpers.StressLoad(_fixture.Client, "/GetServerVariableStress", response => {
                    var text = response.Content.ReadAsStringAsync().Result;
                    Assert.StartsWith("Response Begin", text);
                    Assert.EndsWith("Response End", text);
                });
        }
    }
}
