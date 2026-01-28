// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

public class WorkerClassLibraryTemplateTest : LoggedTest
{
    public WorkerClassLibraryTemplateTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; }
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

    [Fact]
    public async Task WorkerClassLibraryTemplate_CanCreateBuildPublish()
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("workerlib");

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.
        await project.RunDotNetBuildAsync();
    }
}
