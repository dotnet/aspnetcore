// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Items.Test
{
    public class BlazorServerTest
    {
        public BlazorServerTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Output = output;
        }

        public Project Project { get; set; }

        public ProjectFactoryFixture ProjectFactory { get; }
        public ITestOutputHelper Output { get; }

        [Fact]
        [QuarantinedTest]
        public async Task BlazorServerItemTemplate()
        {
            Project = await ProjectFactory.GetOrCreateProject("razorcomponentitem", Output);

            var createResult = await Project.RunDotNetNewAsync("razorcomponent --name Different");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create", Project, createResult));

            Project.AssertFileExists("Different.razor", shouldExist: true);
            Assert.Contains("<h3>Different</h3>", Project.ReadFile("Different.razor"));
        }
    }
}
