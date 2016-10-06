// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.ProjectModel.Tests;
using NuGet.Frameworks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.ProjectModel
{
    public class MsBuildProjectContextBuilderTest : IClassFixture<MsBuildFixture>, IDisposable
    {
        private const string SkipReason = "CI doesn't yet have a new enough version of .NET Core SDK";

        private readonly MsBuildFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly TemporaryFileProvider _files;

        public MsBuildProjectContextBuilderTest(MsBuildFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _files = new TemporaryFileProvider();
        }

        public void Dispose()
        {
            _files.Dispose();
        }

        [Fact(Skip = SkipReason)]
        public void BuildsAllTargetFrameworks()
        {

            _files.Add("test.proj", @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TargetFrameworks>net451;netstandard1.3</TargetFrameworks>
  </PropertyGroup>
</Project>
");
            var contexts = new MsBuildProjectContextBuilder()
                .WithBuildTargets(Array.Empty<string>())
                .WithProjectFile(_files.GetFileInfo("test.proj"))
                .BuildAllTargetFrameworks()
                .ToList();

            Assert.Collection(contexts,
                context =>
                {
                    Assert.Equal(FrameworkConstants.CommonFrameworks.Net451, context.TargetFramework);
                },
                context =>
                {
                    Assert.Equal(FrameworkConstants.CommonFrameworks.NetStandard13, context.TargetFramework);
                });
        }

        [Fact(Skip = SkipReason)]
        public void ExecutesDesignTimeBuild()
        {
            // TODO remove when SDK becomes available on other feeds
            _files.Add("NuGet.config", @"
<configuration>
    <packageSources>
        <clear />
        <add key=""dotnet-core"" value=""https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"" />
        <add key=""dotnet-buildtools"" value=""https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json"" />
        <add key=""nugetbuild"" value=""https://www.myget.org/F/nugetbuild/api/v3/index.json"" />
    </packageSources>
</configuration>");

            _files.Add("test.csproj", @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" />

  <PropertyGroup>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFrameworks>netcoreapp1.0</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NETCore.Sdk"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""Microsoft.NETCore.App"">
      <Version>1.0.1</Version>
    </PackageReference>
  </ItemGroup>

  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
");
            _files.Add("One.cs", "public class Abc {}");
            _files.Add("Two.cs", "public class Abc2 {}");
            _files.Add("Excluded.cs", "public class Abc {}");

            var testContext = _fixture.GetMsBuildContext();

            var muxer = Path.Combine(testContext.ExtensionsPath, "../..", "dotnet.exe");
            var result = Command
                .Create(muxer, new[] { "restore3", Path.Combine(_files.Root, "test.csproj") })
                .OnErrorLine(l => _output.WriteLine(l))
                .OnOutputLine(l => _output.WriteLine(l))
                .Execute();
            Assert.Equal(0, result.ExitCode);

            var expectedCompileItems = new[] { "One.cs", "Two.cs" }.Select(p => Path.Combine(_files.Root, p)).ToArray();

            var context = new MsBuildProjectContextBuilder()
                .AsDesignTimeBuild()
                .UseMsBuild(testContext)
                .WithConfiguration("Debug")
                .WithProjectFile(_files.GetFileInfo("test.csproj"))
                .Build();

            Assert.False(_files.GetFileInfo("bin").Exists);
            Assert.False(_files.GetFileInfo("obj").Exists);
            Assert.Equal(expectedCompileItems, context.CompilationItems.OrderBy(i => i).ToArray());
            Assert.Equal(Path.Combine(_files.Root, "bin", "Debug", "netcoreapp1.0", "test.dll"), context.AssemblyFullPath);
            Assert.True(context.IsClassLibrary);
            Assert.Equal("TestProject", context.ProjectName);
            Assert.Equal(FrameworkConstants.CommonFrameworks.NetCoreApp10, context.TargetFramework);
            Assert.Equal("Microsoft.TestProject", context.RootNamespace);
        }
    }
}
