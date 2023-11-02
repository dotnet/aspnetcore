// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;

namespace Microsoft.Extensions.ApiDescription.Client;

[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/50662")]
public class TargetTest : IDisposable
{
    private static Assembly _assembly = typeof(TargetTest).Assembly;
    private static string _assemblyLocation = Path.GetDirectoryName(_assembly.Location);
    private static string _targetFramework = _assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(m => m.Key == "TargetFramework")
        .Value;

    private ITestOutputHelper _output;
    private TemporaryDirectory _temporaryDirectory;

    public TargetTest(ITestOutputHelper output)
    {
        _output = output;
        _temporaryDirectory = new TemporaryDirectory();

        var build = _temporaryDirectory.SubDir("build");
        var files = _temporaryDirectory.SubDir("files");
        var tasks = _temporaryDirectory.SubDir("tasks").SubDir("netstandard2.0");
        _temporaryDirectory.Create();

        // Populate temporary build folder.
        var directory = new DirectoryInfo(Path.Combine(_assemblyLocation, "build"));
        foreach (var file in directory.GetFiles())
        {
            file.CopyTo(Path.Combine(build.Root, file.Name), overwrite: true);
        }
        directory = new DirectoryInfo(Path.Combine(_assemblyLocation, "TestProjects", "build"));
        foreach (var file in directory.GetFiles())
        {
            file.CopyTo(Path.Combine(build.Root, file.Name), overwrite: true);
        }

        // Populate temporary files folder.
        directory = new DirectoryInfo(Path.Combine(_assemblyLocation, "TestProjects", "files"));
        foreach (var file in directory.GetFiles())
        {
            file.CopyTo(Path.Combine(files.Root, file.Name), overwrite: true);
        }

        // Populate temporary tasks folder.
        directory = new DirectoryInfo(_assemblyLocation);
        foreach (var file in directory.GetFiles("Microsoft.Extensions.ApiDescription.Client.???"))
        {
            file.CopyTo(Path.Combine(tasks.Root, file.Name), overwrite: true);
        }
    }

    [Fact]
    public async Task AddsExpectedItems()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains($"Compile: {Path.Combine("obj", "azureMonitorClient.cs")}", process.Output);
        Assert.Contains($"FileWrites: {Path.Combine("obj", "azureMonitorClient.cs")}", process.Output);
        Assert.DoesNotContain("TypeScriptCompile:", process.Output);
    }

    [Fact]
    public async Task AddsExpectedItems_WithCodeGenerator()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
                CodeGenerator = "NSwagTypeScript",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.DoesNotContain(" Compile:", process.Output);
        Assert.Contains($"FileWrites: {Path.Combine("obj", "azureMonitorClient.ts")}", process.Output);
        Assert.Contains($"TypeScriptCompile: {Path.Combine("obj", "azureMonitorClient.ts")}", process.Output);
    }

    [Fact]
    public async Task AddsExpectedItems_WithMultipleFiles()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json;files/NSwag.json;files/swashbuckle.json",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains($"Compile: {Path.Combine("obj", "azureMonitorClient.cs")}", process.Output);
        Assert.Contains($"Compile: {Path.Combine("obj", "NSwagClient.cs")}", process.Output);
        Assert.Contains($"Compile: {Path.Combine("obj", "swashbuckleClient.cs")}", process.Output);
        Assert.Contains($"FileWrites: {Path.Combine("obj", "azureMonitorClient.cs")}", process.Output);
        Assert.Contains($"FileWrites: {Path.Combine("obj", "NSwagClient.cs")}", process.Output);
        Assert.Contains($"FileWrites: {Path.Combine("obj", "swashbuckleClient.cs")}", process.Output);
        Assert.DoesNotContain("TypeScriptCompile:", process.Output);
    }

    [Fact]
    public async Task AddsExpectedItems_WithMultipleFilesFromGenerator()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
                CodeGenerator = "CustomCSharp",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains($"Compile: {Path.Combine("obj", "azureMonitorClient.cs", "Generated1.cs")}", process.Output);
        Assert.Contains($"Compile: {Path.Combine("obj", "azureMonitorClient.cs", "Generated2.cs")}", process.Output);
        Assert.Contains(
            $"FileWrites: {Path.Combine("obj", "azureMonitorClient.cs", "Generated1.cs")}",
            process.Output);
        Assert.Contains(
            $"FileWrites: {Path.Combine("obj", "azureMonitorClient.cs", "Generated2.cs")}",
            process.Output);
        Assert.DoesNotContain("TypeScriptCompile:", process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "azureMonitorClient.cs")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithOpenApiGenerateCodeOptions()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithProperty("OpenApiGenerateCodeOptions", "--an-option")
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '--an-option' OutputPath: '{Path.Combine("obj", "azureMonitorClient.cs")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithOpenApiCodeDirectory()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithProperty("OpenApiCodeDirectory", "generated")
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("generated", "azureMonitorClient.cs")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithClassName()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
                ClassName = "AzureMonitor"
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);

        // Note ClassName does **not** override OutputPath.
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.AzureMonitor' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "azureMonitorClient.cs")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithCodeGenerator()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
                CodeGenerator = "NSwagTypeScript"
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains(
            "GenerateNSwagTypeScript " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "azureMonitorClient.ts")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithNamespace()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
                Namespace = "SomeNamespace"
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'SomeNamespace.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "azureMonitorClient.cs")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithOptions()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
                Options = "--an-option"
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '--an-option' OutputPath: '{Path.Combine("obj", "azureMonitorClient.cs")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithOutputPath()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
                OutputPath = "Custom.cs"
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);

        // Note OutputPath also overrides ClassName.
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.Custom' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "Custom.cs")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithMultipleFiles()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json;files/NSwag.json;files/swashbuckle.json",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "azureMonitorClient.cs")}'",
            process.Output);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "NSwag.json")} " +
            "Class: 'test.NSwagClient' FirstForGenerator: 'false' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "NSwagClient.cs")}'",
            process.Output);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "swashbuckle.json")} " +
            "Class: 'test.swashbuckleClient' FirstForGenerator: 'false' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "swashbuckleClient.cs")}'",
            process.Output);
    }

    [Fact]
    public async Task ExecutesGeneratorTarget_WithMultipleGenerators()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
            })
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
                CodeGenerator = "NSwagTypeScript"
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.Contains(
            "GenerateNSwagCSharp " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "azureMonitorClient.cs")}'",
            process.Output);
        Assert.Contains(
            "GenerateNSwagTypeScript " +
            $"{Path.Combine(_temporaryDirectory.Root, "files", "azureMonitor.json")} " +
            "Class: 'test.azureMonitorClient' FirstForGenerator: 'true' " +
            $"Options: '' OutputPath: '{Path.Combine("obj", "azureMonitorClient.ts")}'",
            process.Output);
    }

    [Fact]
    public async Task SkipsGeneratorTarget_InSubsequentBuilds()
    {
        // Arrange
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithProperty("OpenApiGenerateCodeOnBuild", "false")
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        // Act 1
        using var firstProcess = await RunBuild();

        // Assert 1 aka Guards
        Assert.Equal(0, firstProcess.ExitCode);
        Assert.Empty(firstProcess.Error);

        // Act 2
        using var secondProcess = await RunBuild();

        // Assert 2
        Assert.Equal(0, secondProcess.ExitCode);
        Assert.Empty(secondProcess.Error);
        Assert.DoesNotContain("GenerateNSwagCSharp ", secondProcess.Output);

        // Act 3
        using var thirdProcess = await RunBuild();

        // Assert 2
        Assert.Equal(0, thirdProcess.ExitCode);
        Assert.Empty(thirdProcess.Error);
        Assert.DoesNotContain("GenerateNSwagCSharp ", thirdProcess.Output);
    }

    [Fact]
    public async Task SkipsGeneratorTarget_WithOpenApiGenerateCodeOnBuild()
    {
        var project = new TemporaryOpenApiProject("test", _temporaryDirectory, "Microsoft.NET.Sdk")
            .WithTargetFrameworks(_targetFramework)
            .WithProperty("OpenApiGenerateCodeOnBuild", "false")
            .WithItem(new TemporaryOpenApiProject.ItemSpec
            {
                Include = "files/azureMonitor.json",
            });
        _temporaryDirectory.WithCSharpProject(project);
        project.Create();

        using var process = await RunBuild();

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(process.Error);
        Assert.DoesNotContain("GenerateNSwagCSharp ", process.Output);
    }

    public void Dispose()
    {
        _temporaryDirectory.Dispose();
    }

    private async Task<ProcessEx> RunBuild()
    {
        var process = ProcessEx.Run(
            _output,
            _temporaryDirectory.Root,
            DotNetMuxer.MuxerPathOrDefault(),
            "build");
        await process.Exited;

        return process;
    }
}
