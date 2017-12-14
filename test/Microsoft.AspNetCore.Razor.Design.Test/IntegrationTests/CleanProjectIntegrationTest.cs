// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class CleanProjectIntegrationTest : MSBuildIntegrationTestBase
    {
        [Fact]
        [InitializeTestProject("SimpleMvc")]
        public async Task CleanProject_RunBuild()
        {
            var result = await DotnetMSBuild("Restore;Build", "/p:RazorCompileOnBuild=true");

            Assert.BuildPassed(result);
            Assert.FileExists(result, @"bin/Debug/netcoreapp2.0/SimpleMvc.dll");
            Assert.FileExists(result, @"bin/Debug/netcoreapp2.0/SimpleMvc.PrecompiledViews.dll");
        }
    }
}
