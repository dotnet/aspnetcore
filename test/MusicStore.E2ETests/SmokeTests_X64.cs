using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    [Trait("E2Etests", "Smoke")]
    public class SmokeTests_X64
    {
        private readonly SmokeTestRunner _smokeTestRunner;

        public SmokeTests_X64(ITestOutputHelper output)
        {
            _smokeTestRunner = new SmokeTestRunner(output);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task SmokeTests_X64_WebListener_Clr()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task SmokeTests_X64_WebListener_CoreClr_Portable()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task SmokeTests_X64_WebListener_CoreClr_Standalone()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task SmokeTests_X64_IISExpress_Clr()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task SmokeTests_X64_IISExpress_CoreClr_Portable()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task SmokeTests_X64_IISExpress_CoreClr_Standalone()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task SmokeTests_X64_Kestrel_Clr()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable);
        }

        [Fact]
        public Task SmokeTests_X64_Kestrel_CoreClr_Portable()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable);
        }

        [Fact]
        public Task SmokeTests_X64_Kestrel_CoreClr_Standalone()
        {
            return _smokeTestRunner.SmokeTestSuite(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone);
        }
    }
}
