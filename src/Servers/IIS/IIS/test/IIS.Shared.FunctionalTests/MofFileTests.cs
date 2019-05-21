// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace IIS.FunctionalTests
{
    public class MofFileTests
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [RequiresIIS(IISCapability.TracingModule)]
        [SkipOnHelix("https://github.com/aspnet/AspNetCore-Internal/issues/2222")]
        public void CheckMofFile()
        {
// This test code needs to be updated to support distributed testing.
// See https://github.com/aspnet/AspNetCore-Internal/issues/2222
#pragma warning disable 0618
            var path = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"), "aspnetcoremodulev2", "aspnetcore", "ancm.mof");
#pragma warning restore 0618
            var process = Process.Start("mofcomp.exe", path);
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }
    }
}
