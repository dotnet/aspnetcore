// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.OpenApi.Build.Tests;

public class GenerateAdditionalXmlFilesForOpenApiTests
{
    private static readonly TimeSpan _defaultProcessTimeout = TimeSpan.FromMinutes(2);
    private const string FutureTfmWorkarounds = """
  <ItemGroup>
    <KnownAppHostPack Include="@(KnownAppHostPack->WithMetadataValue('TargetFramework', 'net11.0'))"
                      TargetFramework="$(TargetFramework)"
                      Condition="!(@(KnownAppHostPack->AnyHaveMetadataValue('TargetFramework', '$(TargetFramework)')))" />
    <KnownRuntimePack Include="@(KnownRuntimePack->WithMetadataValue('TargetFramework', 'net11.0'))"
                      TargetFramework="$(TargetFramework)"
                      Condition="!(@(KnownRuntimePack->AnyHaveMetadataValue('TargetFramework', '$(TargetFramework)')))" />
    <KnownFrameworkReference Include="@(KnownFrameworkReference->WithMetadataValue('TargetFramework', 'net11.0'))"
                             TargetFramework="$(TargetFramework)"
                             Condition="!(@(KnownFrameworkReference->AnyHaveMetadataValue('TargetFramework', '$(TargetFramework)')))" />
  </ItemGroup>
""";

    [Fact]
    public async Task VerifiesTargetGeneratesXmlFiles()
    {
        var projectFile = CreateTestProject();
        var startInfo = new ProcessStartInfo
        {
            FileName = DotNetMuxer.MuxerPathOrDefault(),
            Arguments = "build -t:Build -getItem:AdditionalFiles -p:NETCoreAppMaximumVersion=99.9 -p:LoadPrunePackageDataFromNearestFramework=true -p:AllowMissingPrunePackageData=true",
            WorkingDirectory = Path.GetDirectoryName(projectFile),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeoutTokenSource = new CancellationTokenSource(_defaultProcessTimeout);
        await process.WaitForExitAsync(timeoutTokenSource.Token);

        var output = await standardOutputTask;
        var error = await standardErrorTask;
        Assert.True(process.ExitCode == 0, $"Generated OpenAPI project build failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
        var result = JsonSerializer.Deserialize<ItemsResult>(output);
        var additionalFiles = result.Items.AdditionalFiles;
        Assert.NotEmpty(additionalFiles);

        // Captures ProjectReferences and PackageReferences in project.
        var identities = additionalFiles.Select(x => x["Identity"]).ToArray();
        Assert.Collection(identities,
            x => Assert.EndsWith("ClassLibrary.xml", x)
        );
    }

    private static string CreateTestProject()
    {
        var classLibTempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(classLibTempPath);

        // Create a class library project
        var classLibProjectPath = Path.Combine(classLibTempPath, "ClassLibrary.csproj");
        var classLibProjectContent = $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net12.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

{{FutureTfmWorkarounds}}

</Project>
""";
        File.WriteAllText(classLibProjectPath, classLibProjectContent);

        // Create a class library source file
        var classLibSourcePath = Path.Combine(classLibTempPath, "Class1.cs");
        var classLibSourceContent = """
/// <summary>
/// This is a class
/// </summary>
public class Class1
{
}
""";
        File.WriteAllText(classLibSourcePath, classLibSourceContent);

        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);

        // Copy the targets file to the temp directory
        var sourceTargetsPath = Path.Combine(AppContext.BaseDirectory, "Microsoft.AspNetCore.OpenApi.targets");
        var targetTargetsPath = Path.Combine(tempPath, "Microsoft.AspNetCore.OpenApi.targets");
        File.Copy(sourceTargetsPath, targetTargetsPath);

        var projectPath = Path.Combine(tempPath, "TestProject.csproj");
        var projectContent = $$"""
<Project Sdk="Microsoft.NET.Sdk.Web">
    <Import Project="{{targetTargetsPath}}" />

  <PropertyGroup>
    <TargetFramework>net12.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

{{FutureTfmWorkarounds}}

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <ProjectReference Include="{{classLibProjectPath}}" />
  </ItemGroup>
</Project>
""";
        File.WriteAllText(projectPath, projectContent);

        // Create a test source file
        var sourcePath = Path.Combine(tempPath, "Program.cs");
        var sourceContent = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create(args);

app.MapGet("/", () => "Hello World!");

app.Run();
""";
        File.WriteAllText(sourcePath, sourceContent);

        return projectPath;
    }

    private record ItemsResult(AdditionalFilesResult Items);
    private record AdditionalFilesResult(Dictionary<string, string>[] AdditionalFiles);
}
