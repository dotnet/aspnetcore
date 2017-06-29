using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests.SmokeTestsUsingStore
{
    public class SmokeTests : IClassFixture<StoreSetupFixture>
    {
        private readonly StoreSetupFixture _testFixture;
        private readonly ITestOutputHelper _output;

        public SmokeTests(
            StoreSetupFixture testFixure,
            ITestOutputHelper output)
        {
            _testFixture = testFixure;
            _output = output;
        }

        [SkipIfEnvironmentVariableNotEnabled("RUN_RUNTIME_STORE_TESTS")]
        [ConditionalFact]
        [Trait("smoketests", "usestore")]
        public async Task DefaultLocation_Kestrel()
        {
            var tests = new TestHelper(_output);
            await tests.SmokeTestSuite(
                ServerType.Kestrel,
                _testFixture.StoreDirectory);
        }

        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [SkipIfEnvironmentVariableNotEnabled("RUN_RUNTIME_STORE_TESTS")]
        [ConditionalFact]
        [Trait("smoketests", "usestore")]
        public async Task DefaultLocation_WebListener()
        {
            var tests = new TestHelper(_output);
            await tests.SmokeTestSuite(
                ServerType.WebListener,
                _testFixture.StoreDirectory);
        }
    }
}