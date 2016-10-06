// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.ProjectModel.Tests;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.Extensions.ProjectModel.MsBuild
{
    public class MsBuildProjectDependencyProviderTests: IClassFixture<MsBuildFixture>
    {
        private const string NugetConfigTxt = @"
<configuration>
    <packageSources>
        <clear />
        <add key=""NuGet"" value=""https://api.nuget.org/v3/index.json"" />
        <add key=""dotnet-core"" value=""https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"" />
        <add key=""dotnet-buildtools"" value=""https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json"" />
        <add key=""nugetbuild"" value=""https://www.myget.org/F/nugetbuild/api/v3/index.json"" />
    </packageSources>
</configuration>";

        private const string RootProjectTxt = @"
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
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""Microsoft.NETCore.Sdk"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""Microsoft.NETCore.App"">
      <Version>1.0.1</Version>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Library1\library.csproj"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include = ""xyz.dll"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
";

        private const string LibraryProjectTxt = @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" />

  <PropertyGroup>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
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
";
        private readonly MsBuildFixture _fixture;
        private readonly ITestOutputHelper _output;

        public MsBuildProjectDependencyProviderTests(MsBuildFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact(Skip = "CI doesn't yet have a new enough version of .NET Core SDK")]
        public void BuildDependenciesForProject()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
                Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
                // TODO remove when SDK becomes available on other feeds
                fileProvider.Add("NuGet.config", NugetConfigTxt);

                // Add Root Project
                fileProvider.Add($"Root{Path.DirectorySeparatorChar}test.csproj", RootProjectTxt);
                fileProvider.Add($"Root{Path.DirectorySeparatorChar}One.cs", "public class Abc {}");
                fileProvider.Add($"Root{Path.DirectorySeparatorChar}Two.cs", "public class Abc2 {}");
                fileProvider.Add($"Root{Path.DirectorySeparatorChar}Excluded.cs", "public class Abc {}");

                // Add Class Library project
                fileProvider.Add($"Library1{Path.DirectorySeparatorChar}library.csproj", LibraryProjectTxt);
                fileProvider.Add($"Library1{Path.DirectorySeparatorChar}Three.cs", "public class Abc3 {}");

                var testContext = _fixture.GetMsBuildContext();

                var muxer = Path.Combine(testContext.ExtensionsPath, "../..", "dotnet.exe");
                var result = Command
                    .Create(muxer, new[] { "restore3", Path.Combine(fileProvider.Root, "Library1","Library1.csproj") })
                    .OnErrorLine(l => _output.WriteLine(l))
                    .OnOutputLine(l => _output.WriteLine(l))
                    .Execute();

                Assert.Equal(0, result.ExitCode);

                result = Command
                    .Create(muxer, new[] { "restore3", Path.Combine(fileProvider.Root, "Root", "test.csproj") })
                    .OnErrorLine(l => _output.WriteLine(l))
                    .OnOutputLine(l => _output.WriteLine(l))
                    .Execute();

                Assert.Equal(0, result.ExitCode);

                var builder = new MsBuildProjectContextBuilder()
                    .AsDesignTimeBuild()
                    .UseMsBuild(testContext)
                    .WithTargetFramework(FrameworkConstants.CommonFrameworks.NetCoreApp10)
                    .WithConfiguration("Debug")
                    .WithProjectFile(fileProvider.GetFileInfo(Path.Combine("Root", "test.csproj")));

                var context = builder.Build();

                var compilationAssemblies = context.CompilationAssemblies;
                var lib1Dll = compilationAssemblies
                    .Where(assembly => assembly.Name.Equals("Library1", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                Assert.NotNull(lib1Dll);
                var expectedPath = Path.Combine(fileProvider.Root, "Root", "bin", "Debug", "netcoreapp1.0", "lib1.dll");
                Assert.Equal(expectedPath, lib1Dll.ResolvedPath, ignoreCase: true);
                
                // This reference doesn't resolve so should not be available here.
                Assert.False(compilationAssemblies.Any(assembly => assembly.Name.Equals("xyz", StringComparison.OrdinalIgnoreCase)));

                var packageDependencies = context.PackageDependencies;
                var mvcPackage = packageDependencies
                    .Where(p => p.Name.Equals("Microsoft.AspNetCore.Mvc", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                Assert.NotNull(mvcPackage);

                Assert.True(mvcPackage.Dependencies.Any(dependency => dependency.Name.Equals("Microsoft.Extensions.DependencyInjection", StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
