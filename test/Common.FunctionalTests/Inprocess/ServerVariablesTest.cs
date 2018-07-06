// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
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
            Assert.Equal("THIS_VAR_IS_UNDEFINED: (null)", await _fixture.Client.GetStringAsync("/ServerVariable?q=THIS_VAR_IS_UNDEFINED"));
        }

        [ConditionalFact]
        public async Task BasePathIsNotPrefixedBySlashSlashQuestionMark()
        {
            Assert.DoesNotContain(@"\\?\", await _fixture.Client.GetStringAsync("/BasePath"));
        }

        [ConditionalFact]
        public async Task GetServerVariableDoesNotCrash()
        {
            async Task RunRequests()
            {
                var client = new HttpClient() { BaseAddress = _fixture.Client.BaseAddress };

                for (int j = 0; j < 10; j++)
                {
                    var response = await client.GetStringAsync("/GetServerVariableStress");
                    Assert.StartsWith("Response Begin", response);
                    Assert.EndsWith("Response End", response);
                }
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(RunRequests));
            }

            await Task.WhenAll(tasks);
        }
    }
}
