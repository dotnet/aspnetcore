// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System.Reflection;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class MsBuildGenerator : DotNetCliGenerator
    {
        public MsBuildGenerator()
            : base("netcoreapp1.1", null, (Func<Platform, string>) (_ => "x64"), null, null)
        {
        }

        protected override string GetProjectFilePath(string binariesDirectoryPath)
            => Path.Combine(binariesDirectoryPath, "Benchmark.Generated.csproj");

        protected override void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
        {
            var projectTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"" ToolsVersion=""15.0"">

  <Import Project=""..\build\common.props"" />

  <PropertyGroup>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""$CODEFILENAME$"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\test\Microsoft.AspNetCore.Server.Kestrel.Performance\Microsoft.AspNetCore.Server.Kestrel.Performance.csproj"" />
  </ItemGroup>

</Project>";

            var projectContent = SetCodeFileName(projectTemplate, Path.GetFileName(artifactsPaths.ProgramCodePath));
            File.WriteAllText(artifactsPaths.ProjectFilePath, projectContent);

            var runtimeConfigContent = @"
{
  ""configProperties"": {
    ""System.GC.Server"": true
  }
}";
            File.WriteAllText(Path.Combine(artifactsPaths.BuildArtifactsDirectoryPath, "runtimeConfig.template.json"), runtimeConfigContent);
        }
    }
}