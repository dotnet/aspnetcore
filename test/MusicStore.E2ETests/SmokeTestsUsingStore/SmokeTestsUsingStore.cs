using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class SmokeTestsUsingDefaultLocation : IClassFixture<DefaultLocationSetupFixture>
    {
        private readonly DefaultLocationSetupFixture _testFixture;
        private readonly ITestOutputHelper _output;

        public SmokeTestsUsingDefaultLocation(
            DefaultLocationSetupFixture testFixure,
            ITestOutputHelper output)
        {
            _testFixture = testFixure;
            _output = output;
        }

        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [EnvironmentVariableSkipCondition(Store.MusicStoreAspNetCoreStoreFeed, null, SkipOnMatch = false)]
        [ConditionalTheory]
        [Trait("smoketests", "usestore")]
        [Trait("smoketests", "usestore-defaultlocation")]
        [InlineData(ServerType.Kestrel)]
        [InlineData(ServerType.WebListener)]
        public async Task DefaultLocation(ServerType serverType)
        {
            var tests = new SmokeTestsUsingStoreHelper(_output);
            await tests.SmokeTestSuite(
                serverType,
                _testFixture.CreateStoreInDefaultLocation,
                _testFixture.StoreDirectory);
        }
    }

    public class SmokeTestsUsingInCustomLocation : IClassFixture<CustomLocationSetupFixture>
    {
        private readonly CustomLocationSetupFixture _testFixture;
        private readonly ITestOutputHelper _output;

        public SmokeTestsUsingInCustomLocation(
            CustomLocationSetupFixture testFixure,
            ITestOutputHelper output)
        {
            _testFixture = testFixure;
            _output = output;
        }

        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [EnvironmentVariableSkipCondition(Store.MusicStoreAspNetCoreStoreFeed, null, SkipOnMatch = false)]
        [ConditionalTheory]
        [Trait("smoketests", "usestore")]
        [Trait("smoketests", "usestore-customlocation")]
        [InlineData(ServerType.Kestrel)]
        [InlineData(ServerType.WebListener)]
        public async Task CustomLocation(ServerType serverType)
        {
            var tests = new SmokeTestsUsingStoreHelper(_output);
            await tests.SmokeTestSuite(
                serverType,
                _testFixture.CreateStoreInDefaultLocation,
                _testFixture.StoreDirectory);
        }
    }
}