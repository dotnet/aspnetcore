// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class SpaTemplatesTest : LoggedTest
{
    public SpaTemplatesTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; set; }

    private ITestOutputHelper _output;
    public ITestOutputHelper Output
    {
        get
        {
            if (_output == null)
            {
                _output = new TestOutputLogger(Logger);
            }
            return _output;
        }
    }

    [Theory]
    [InlineData("angularind", "angular", "Individual")]
    [InlineData("reactind", "react", "Individual")]
    [InlineData("angularnoauth", "angular", null)]
    [InlineData("reactnoauth", "react", null)]
    public async Task SpaTemplates_BuildAndPublish(string projectKey, string template, string auth)
    {
        var project = await ProjectFactory.GetOrCreateProject(projectKey, Output);
        var args = new[] { "--NoSpaFrontEnd", "true" };
        var createResult = await project.RunDotNetNewAsync(template, auth: auth, args: args);
        Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage(template, project, createResult));

        var publishResult = await project.RunDotNetPublishAsync();
        Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        var buildResult = await project.RunDotNetBuildAsync();
        Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));
    }
}
