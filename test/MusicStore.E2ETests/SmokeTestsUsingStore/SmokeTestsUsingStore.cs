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

        [EnvironmentVariableSkipCondition(Store.MusicStoreAspNetCoreStoreFeed, null, SkipOnMatch = false)]
        [ConditionalFact]
        [Trait("smoketests", "usestore")]
        [Trait("smoketests", "usestore-defaultlocation")]
        public async Task DefaultLocation_Kestrel()
        {
            var tests = new SmokeTestsUsingStoreHelper(_output);
            await tests.SmokeTestSuite(
                ServerType.Kestrel,
                _testFixture.CreateStoreInDefaultLocation,
                _testFixture.StoreDirectory);
        }

        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [EnvironmentVariableSkipCondition(Store.MusicStoreAspNetCoreStoreFeed, null, SkipOnMatch = false)]
        [ConditionalFact]
        [Trait("smoketests", "usestore")]
        [Trait("smoketests", "usestore-defaultlocation")]
        public async Task DefaultLocation_WebListener()
        {
            var tests = new SmokeTestsUsingStoreHelper(_output);
            await tests.SmokeTestSuite(
                ServerType.WebListener,
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

        [EnvironmentVariableSkipCondition(Store.MusicStoreAspNetCoreStoreFeed, null, SkipOnMatch = false)]
        [ConditionalFact]
        [Trait("smoketests", "usestore")]
        [Trait("smoketests", "usestore-customlocation")]
        public async Task CustomLocation_Kestrel()
        {
            var tests = new SmokeTestsUsingStoreHelper(_output);
            await tests.SmokeTestSuite(
                ServerType.Kestrel,
                _testFixture.CreateStoreInDefaultLocation,
                _testFixture.StoreDirectory);
        }

        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [EnvironmentVariableSkipCondition(Store.MusicStoreAspNetCoreStoreFeed, null, SkipOnMatch = false)]
        [ConditionalFact]
        [Trait("smoketests", "usestore")]
        [Trait("smoketests", "usestore-customlocation")]
        public async Task CustomLocation_WebListener()
        {
            var tests = new SmokeTestsUsingStoreHelper(_output);
            await tests.SmokeTestSuite(
                ServerType.WebListener,
                _testFixture.CreateStoreInDefaultLocation,
                _testFixture.StoreDirectory);
        }
    }
}