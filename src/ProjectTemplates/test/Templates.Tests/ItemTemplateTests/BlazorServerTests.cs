// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Items.Test;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

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
    public async Task BlazorServerItemTemplate()
    {
        Project = await ProjectFactory.CreateProject(Output);

        await Project.RunDotNetNewAsync("razorcomponent --name Different", isItemTemplate: true);

        Project.AssertFileExists("Different.razor", shouldExist: true);
        Project.AssertFileExists("Different.razor.cs", shouldExist: false);
        Assert.Contains("<h3>Different</h3>", Project.ReadFile("Different.razor"));
    }

    [Fact]
    public async Task BlazorServerItemTemplateWithCodeBehind()
    {
        Project = await ProjectFactory.CreateProject(Output);

        await Project.RunDotNetNewAsync("razorcomponent --name CodeBehindComponent --ExcludeCodeBehind false", isItemTemplate: true);

        Project.AssertFileExists("CodeBehindComponent.razor", shouldExist: true);
        Project.AssertFileExists("CodeBehindComponent.razor.cs", shouldExist: true);
        
        var razorContent = Project.ReadFile("CodeBehindComponent.razor");
        var codeContent = Project.ReadFile("CodeBehindComponent.razor.cs");
        
        Assert.Contains("<h3>CodeBehindComponent</h3>", razorContent);
        Assert.Contains("public partial class CodeBehindComponent : ComponentBase", codeContent);
        Assert.Contains("using Microsoft.AspNetCore.Components;", codeContent);
    }
}
