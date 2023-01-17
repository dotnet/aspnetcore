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
    [InlineData("angular", "Individual")]
    [InlineData("react", "Individual")]
    [InlineData("angular", null)]
    [InlineData("react", null)]
    public async Task SpaTemplates_BuildAndPublish(string template, string auth)
    {
        var project = await ProjectFactory.CreateProject(Output);
        var args = new[] { "--NoSpaFrontEnd", "true" };
        await project.RunDotNetNewAsync(template, auth: auth, args: args);

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();
    }
}
