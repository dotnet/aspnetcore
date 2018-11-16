// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    [Trait("E2Etests", "DotnetRun")]
    public class DotnetRunTests_X64
    {
        private readonly DotnetRunTestRunner _testRunner;

        public DotnetRunTests_X64(ITestOutputHelper output)
        {
            _testRunner = new DotnetRunTestRunner(output);
        }

        [Fact]
        public Task DotnetRunTests_X64_Kestrel_CoreClr()
        {
            return RunTests(ServerType.Kestrel, RuntimeFlavor.CoreClr, ApplicationType.Portable);
        }
#if !NETCOREAPP2_0 // Avoid running CLR based tests once on netcoreapp2.0 and netcoreapp2.1 each
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public Task DotnetRunTests_X64_Kestrel_Clr()
        {
            // CLR must be published as standalone to perform rid specific deployment
            return RunTests(ServerType.Kestrel, RuntimeFlavor.Clr, ApplicationType.Standalone);
        }
#endif
        private Task RunTests(ServerType serverType, RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
            => _testRunner.RunTests(serverType, runtimeFlavor, applicationType, RuntimeArchitecture.x64);
    }
}
