// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.Extensions.ProjectModel
{
    public class MsBuildProjectContextBuilderTest : IClassFixture<MsBuildFixture>
    {
        private const string SkipReason = "CI doesn't yet have a new enough version of .NET Core SDK";

        private readonly MsBuildFixture _fixture;

        public MsBuildProjectContextBuilderTest(MsBuildFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact(Skip = SkipReason)]
        public void ExecutesDesignTimeBuild()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                // TODO When .NET Core SDK is available, detect and add to this test project
                // fileProvider.Add("test.nuget.targets", "Import .NET Core SDK here");
                fileProvider.Add("test.csproj", @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" />

  <PropertyGroup>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFrameworkIdentifier>.NETCoreApp</TargetFrameworkIdentifier>
    <TargetFrameworkVersion>v1.0</TargetFrameworkVersion>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
");
                fileProvider.Add("One.cs", "public class Abc {}");
                fileProvider.Add("Two.cs", "public class Abc2 {}");
                fileProvider.Add("Excluded.cs", "public class Abc {}");

                var testContext = _fixture.GetMsBuildContext();

                var expectedCompileItems = new[] { "One.cs", "Two.cs" }.Select(p => Path.Combine(fileProvider.Root, p)).ToArray();
                var builder = new MsBuildProjectContextBuilder()
                    .WithMsBuild(testContext)
                    .WithDesignTimeBuild()
                    // In latest version of MSBuild, setting this property causes evaluation errors when SDK is not available
                    //.WithConfiguration("Debug")
                    .WithProjectFile(fileProvider.GetFileInfo("test.csproj"));

                // TODO remove ignoreBuildErrors flag
                // this always throws because Microsoft.NETCore.SDK is not available.
                var context = builder.Build(ignoreBuildErrors: true);

                Assert.False(fileProvider.GetFileInfo("bin").Exists);
                Assert.False(fileProvider.GetFileInfo("obj").Exists);
                Assert.Equal(expectedCompileItems, context.CompilationItems.OrderBy(i => i).ToArray());
                Assert.Equal(Path.Combine(fileProvider.Root, "bin", "Debug", "test.dll"), context.AssemblyFullPath);
                Assert.True(context.IsClassLibrary);
                Assert.Equal("TestProject", context.ProjectName);
                Assert.Equal(FrameworkConstants.CommonFrameworks.NetCoreApp10, context.TargetFramework);
                Assert.Equal("Microsoft.TestProject", context.RootNamespace);
            }
        }
    }
}
