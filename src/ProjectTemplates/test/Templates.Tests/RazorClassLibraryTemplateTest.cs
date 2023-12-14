// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class RazorClassLibraryTemplateTest : LoggedTest
{
    public RazorClassLibraryTemplateTest(ProjectFactoryFixture projectFactory)
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
    public async Task RazorClassLibraryTemplate_WithViews_Async()
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("razorclasslib", args: new[] { "--support-pages-and-views", "true" });

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task RazorClassLibraryTemplateAsync()
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("razorclasslib");

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();
    }
}
