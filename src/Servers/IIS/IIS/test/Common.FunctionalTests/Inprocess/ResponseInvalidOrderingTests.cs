// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISTestSiteCollection.Name)]
    public class ResponseInvalidOrderingTest
    {
        private readonly IISTestSiteFixture _fixture;

        public ResponseInvalidOrderingTest(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalTheory]
        [InlineData("SetStatusCodeAfterWrite")]
        [InlineData("SetHeaderAfterWrite")]
        public async Task ResponseInvalidOrderingTests_ExpectFailure(string path)
        {
            Assert.Equal($"Started_{path}Threw_Finished", await _fixture.Client.GetStringAsync("/ResponseInvalidOrdering/" + path));
        }
    }
}
