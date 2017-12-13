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
            var result = await DotnetMSBuild("Restore;Build"); // Equivalent to dotnet build

            Assert.BuildPassed(result);
        }
    }
}
