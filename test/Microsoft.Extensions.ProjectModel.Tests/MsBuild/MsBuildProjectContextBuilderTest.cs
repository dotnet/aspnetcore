// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.ProjectModel.Tests;
using NuGet.Frameworks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.ProjectModel
{
    public class MsBuildProjectContextBuilderTest : IClassFixture<MsBuildFixture>
    {
        private const string SkipReason = "CI doesn't yet have a new enough version of .NET Core SDK";

        private readonly MsBuildFixture _fixture;
        private readonly ITestOutputHelper _output;

        public MsBuildProjectContextBuilderTest(MsBuildFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact(Skip = SkipReason)]
        public void ExecutesDesignTimeBuild()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                // TODO remove when SDK becomes available on other feeds
                fileProvider.Add("NuGet.config", @"
<configuration>
    <packageSources>
        <clear />
        <add key=""dotnet-core"" value=""https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"" />
        <add key=""dotnet-buildtools"" value=""https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json"" />
        <add key=""nugetbuild"" value=""https://www.myget.org/F/nugetbuild/api/v3/index.json"" />
    </packageSources>
</configuration>");

                fileProvider.Add("test.csproj", @"
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
                fileProvider.Add("One.cs", "public class Abc {}");
                fileProvider.Add("Two.cs", "public class Abc2 {}");
                fileProvider.Add("Excluded.cs", "public class Abc {}");

                var testContext = _fixture.GetMsBuildContext();

                var muxer = Path.Combine(testContext.ExtensionsPath, "../..", "dotnet.exe");
                var result = Command
                    .Create(muxer, new[] { "restore3", Path.Combine(fileProvider.Root, "test.csproj") })
                    .OnErrorLine(l => _output.WriteLine(l))
                    .OnOutputLine(l => _output.WriteLine(l))
                    .Execute();
                Assert.Equal(0, result.ExitCode);

                var expectedCompileItems = new[] { "One.cs", "Two.cs" }.Select(p => Path.Combine(fileProvider.Root, p)).ToArray();
                var builder = new MsBuildProjectContextBuilder()
                    .AsDesignTimeBuild()
                    .UseMsBuild(testContext)
                    .WithTargetFramework(FrameworkConstants.CommonFrameworks.NetCoreApp10)
                    .WithConfiguration("Debug")
                    .WithProjectFile(fileProvider.GetFileInfo("test.csproj"));

                var context = builder.Build();

                Assert.False(fileProvider.GetFileInfo("bin").Exists);
                Assert.False(fileProvider.GetFileInfo("obj").Exists);
                Assert.Equal(expectedCompileItems, context.CompilationItems.OrderBy(i => i).ToArray());
                Assert.Equal(Path.Combine(fileProvider.Root, "bin", "Debug", "netcoreapp1.0", "test.dll"), context.AssemblyFullPath);
                Assert.True(context.IsClassLibrary);
                Assert.Equal("TestProject", context.ProjectName);
                Assert.Equal(FrameworkConstants.CommonFrameworks.NetCoreApp10, context.TargetFramework);
                Assert.Equal("Microsoft.TestProject", context.RootNamespace);
            }
        }
    }
}
