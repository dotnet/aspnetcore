#if NETCOREAPP2_1 // Avoid running CLR based tests once on netcoreapp2.0 and netcoreapp2.1 each
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    [Trait("E2Etests", "PublishAndRun")]
    public class PublishAndRunTests_X86
    {
        private readonly PublishAndRunTestRunner _testRunner;

        public PublishAndRunTests_X86(ITestOutputHelper output)
        {
            _testRunner = new PublishAndRunTestRunner(output);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task PublishAndRunTests_X86_WebListener_Clr()
        {
            // CLR must be published as standalone to perform rid specific deployment
            return RunTests(ServerType.WebListener, RuntimeFlavor.Clr, ApplicationType.Standalone);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task PublishAndRunTests_X86_Kestrel_Clr()
        {
            // CLR must be published as standalone to perform rid specific deployment
            return RunTests(ServerType.Kestrel, RuntimeFlavor.Clr, ApplicationType.Standalone);
        }

        private Task RunTests(ServerType serverType, RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
            => _testRunner.RunTests(serverType, runtimeFlavor, applicationType, RuntimeArchitecture.x86);
    }
}
#endif