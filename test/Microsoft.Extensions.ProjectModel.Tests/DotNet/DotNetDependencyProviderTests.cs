// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.ProjectModel.DotNet
{
    public class DotNetDependencyProviderTests
    {
        private const string projectJson = @"
{
  ""buildOptions"": {
  },
  ""dependencies"": {
    ""Microsoft.AspNetCore.Mvc"": ""1.0.0-*"",
  },
  ""frameworks"": {
    ""netcoreapp1.0"": {
      ""dependencies"": {
        ""Microsoft.NETCore.App"": {
          ""version"": ""1.0.0"",
          ""type"": ""platform""
        }
      }
    }
  },
}
";
        private readonly ITestOutputHelper _output;

        public DotNetDependencyProviderTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BuildProjectDependencies()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                Directory.CreateDirectory(Path.Combine(fileProvider.Root, "demo"));
                fileProvider.Add($"demo{Path.DirectorySeparatorChar}project.json", projectJson);
                fileProvider.Add($"demo{Path.DirectorySeparatorChar}First.cs", "namespace demo { class First{} }");

                var muxer = new Muxer().MuxerPath;

                var result = Command
                    .Create(muxer, new[] { "restore", Path.Combine(fileProvider.Root, "demo") })
                    .OnErrorLine(l => _output.WriteLine(l))
                    .OnOutputLine(l => _output.WriteLine(l))
                    .Execute();

                Assert.Equal(0, result.ExitCode);
                var oldContext = ProjectContext
                    .CreateContextForEachFramework(Path.Combine(fileProvider.Root, "demo", "project.json"))
                    .First();

                var context = new DotNetProjectContext(oldContext, "Debug", Path.Combine(fileProvider.Root, "demo", "bin"));

                var assembly = context
                    .CompilationAssemblies
                    .Where(asm => asm.Name.Equals("Microsoft.AspNetCore.Mvc", StringComparison.OrdinalIgnoreCase))
                    .First();

                Assert.True(File.Exists(assembly.ResolvedPath));
                Assert.True(assembly.ResolvedPath.EndsWith("Microsoft.AspNetCore.Mvc.dll", StringComparison.OrdinalIgnoreCase));

                var mvcPackage = context
                    .PackageDependencies
                    .Where(package => package.Name.Equals("Microsoft.AspNetCore.Mvc", StringComparison.OrdinalIgnoreCase))
                    .First();

                Assert.True(Directory.Exists(mvcPackage.Path));
                Assert.True(mvcPackage.Path.EndsWith($"Microsoft.AspNetCore.Mvc{Path.DirectorySeparatorChar}1.0.0", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
