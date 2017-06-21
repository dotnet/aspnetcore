using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    [Trait("E2Etests", "PublishAndRun")]
    public class PublishAndRunTests_X64
    {
        private readonly PublishAndRunTestRunner _testRunner;

        public PublishAndRunTests_X64(ITestOutputHelper output)
        {
            _testRunner = new PublishAndRunTestRunner(output);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task PublishAndRunTests_X64_WebListener_CoreCLR_Portable()
        {
            return RunTests(ServerType.WebListener, RuntimeFlavor.CoreClr, ApplicationType.Portable);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task PublishAndRunTests_X64_WebListener_CoreCLR_Standalone()
        {
            return RunTests(ServerType.WebListener, RuntimeFlavor.CoreClr, ApplicationType.Standalone);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task PublishAndRunTests_X64_WebListener_Clr()
        {
            // CLR must be published as standalone to perform rid specific deployment
            return RunTests(ServerType.WebListener, RuntimeFlavor.Clr, ApplicationType.Standalone);
        }

        [Fact]
        public Task PublishAndRunTests_X64_Kestrel_CoreClr_Portable()
        {
            return RunTests(ServerType.Kestrel, RuntimeFlavor.CoreClr, ApplicationType.Portable);
        }

        [Fact]
        public Task PublishAndRunTests_X64_Kestrel_CoreClr_Standalone()
        {
            return RunTests(ServerType.Kestrel, RuntimeFlavor.CoreClr, ApplicationType.Standalone);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task PublishAndRunTests_X64_Kestrel_Clr()
        {
            // CLR must be published as standalone to perform rid specific deployment
            return RunTests(ServerType.Kestrel, RuntimeFlavor.Clr, ApplicationType.Standalone);
        }

        private Task RunTests(ServerType serverType, RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
            => _testRunner.RunTests(serverType, runtimeFlavor, applicationType, RuntimeArchitecture.x64);
    }
}
