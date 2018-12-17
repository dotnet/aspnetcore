// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    public class EnvironmentVariableTests: FixtureLoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        public EnvironmentVariableTests(IISTestSiteFixture fixture): base(fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task GetUniqueEnvironmentVariable()
        {
            Assert.Equal("foobar", await _fixture.Client.GetStringAsync("/CheckEnvironmentVariable"));
        }

        [ConditionalFact]
        public async Task GetLongEnvironmentVariable()
        {
            Assert.Equal(
                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative" +
                "AReallyLongValueThatIsGreaterThan300CharactersToForceResizeInNative",
                await _fixture.Client.GetStringAsync("/CheckEnvironmentLongValueVariable"));
        }

        [ConditionalFact]
        public async Task GetExistingEnvironmentVariable()
        {
            Assert.Contains(";foobarbaz", await _fixture.Client.GetStringAsync("/CheckAppendedEnvironmentVariable"));
        }

        [ConditionalFact]
        public async Task AuthHeaderEnvironmentVariableRemoved()
        {
            Assert.DoesNotContain("shouldberemoved", await _fixture.Client.GetStringAsync("/CheckRemoveAuthEnvironmentVariable"));
        }
    }
}
