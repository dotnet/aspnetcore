// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests.SmokeTestsUsingStore
{
    public class SmokeTests : LoggedTest
    {
        private readonly ITestOutputHelper _output;

        public SmokeTests(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        [SkipIfEnvironmentVariableNotEnabled("RUN_RUNTIME_STORE_TESTS")]
        [ConditionalFact]
        [Trait("smoketests", "usestore")]
        public async Task DefaultLocation_Kestrel()
        {
            var tests = new TestHelper(_output);
            await tests.SmokeTestSuite(ServerType.Kestrel);
        }
    }
}