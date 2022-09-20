// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class WorkerTemplateTest : LoggedTest
{
    public WorkerTemplateTest(ProjectFactoryFixture projectFactory)
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

    [ConditionalTheory]
    [InlineData("C#", null)]
    [InlineData("C#", new [] { ArgConstants.UseProgramMain })]
    [InlineData("F#", null)]
    public async Task WorkerTemplateAsync(string language, string[] args)
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("worker", language: language, args: args);

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(hasListeningUri: false))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync(hasListeningUri: false))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));
        }
    }
}
