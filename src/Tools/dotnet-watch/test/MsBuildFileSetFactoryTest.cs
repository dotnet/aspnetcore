// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.Tests
{
    using ItemSpec = TemporaryCSharpProject.ItemSpec;

    public class MsBuildFileSetFactoryTest : IDisposable
    {
        private readonly IReporter _reporter;
        private readonly TemporaryDirectory _tempDir;
        public MsBuildFileSetFactoryTest(ITestOutputHelper output)
        {
            _reporter = new TestReporter(output);
            _tempDir = new TemporaryDirectory();
        }

        [Fact]
        public async Task FindsCustomWatchItems()
        {
            _tempDir
                .WithCSharpProject("Project1", out var target)
                    .WithTargetFrameworks("netcoreapp1.0")
                    .WithItem(new ItemSpec { Name = "Watch", Include = "*.js", Exclude = "gulpfile.js" })
                    .Dir()
                    .WithFile("Program.cs")
                    .WithFile("app.js")
                    .WithFile("gulpfile.js");

            var fileset = await GetFileSet(target);

            AssertEx.EqualFileList(
                _tempDir.Root,
                new[]
                {
                    "Project1.csproj",
                    "Program.cs",
                    "app.js"
                },
                fileset
            );
        }

        [Fact]
        public async Task ExcludesDefaultItemsWithWatchFalseMetadata()
        {
            _tempDir
                .WithCSharpProject("Project1", out var target)
                    .WithTargetFrameworks("net40")
                    .WithItem(new ItemSpec { Name = "EmbeddedResource", Update = "*.resx", Watch = false })
                    .Dir()
                    .WithFile("Program.cs")
                    .WithFile("Strings.resx");

            var fileset = await GetFileSet(target);

            AssertEx.EqualFileList(
                _tempDir.Root,
                new[]
                {
                    "Project1.csproj",
                    "Program.cs",
                },
                fileset
            );
        }

        [Fact]
        public async Task SingleTfm()
        {
            _tempDir
                .SubDir("src")
                    .SubDir("Project1")
                        .WithCSharpProject("Project1", out var target)
                        .WithProperty("BaseIntermediateOutputPath", "obj")
                        .WithTargetFrameworks("netcoreapp1.0")
                        .Dir()
                        .WithFile("Program.cs")
                        .WithFile("Class1.cs")
                        .SubDir("obj").WithFile("ignored.cs").Up()
                        .SubDir("Properties").WithFile("Strings.resx").Up()
                    .Up()
                .Up()
                .Create();

            var fileset = await GetFileSet(target);

            AssertEx.EqualFileList(
                _tempDir.Root,
                new[]
                {
                    "src/Project1/Project1.csproj",
                    "src/Project1/Program.cs",
                    "src/Project1/Class1.cs",
                    "src/Project1/Properties/Strings.resx",
                },
                fileset
            );
        }

        [Fact]
        public async Task MultiTfm()
        {
            _tempDir
                .SubDir("src")
                    .SubDir("Project1")
                        .WithCSharpProject("Project1", out var target)
                        .WithTargetFrameworks("netcoreapp1.0", "net451")
                        .WithProperty("EnableDefaultCompileItems", "false")
                        .WithItem("Compile", "Class1.netcore.cs", "'$(TargetFramework)'=='netcoreapp1.0'")
                        .WithItem("Compile", "Class1.desktop.cs", "'$(TargetFramework)'=='net451'")
                        .Dir()
                        .WithFile("Class1.netcore.cs")
                        .WithFile("Class1.desktop.cs")
                        .WithFile("Class1.notincluded.cs")
                    .Up()
                .Up()
                .Create();

            var fileset = await GetFileSet(target);

            AssertEx.EqualFileList(
                _tempDir.Root,
                new[]
                {
                    "src/Project1/Project1.csproj",
                    "src/Project1/Class1.netcore.cs",
                    "src/Project1/Class1.desktop.cs",
                },
                fileset
            );
        }

        [Fact]
        public async Task ProjectReferences_OneLevel()
        {
            _tempDir
                .SubDir("src")
                    .SubDir("Project2")
                        .WithCSharpProject("Project2", out var proj2)
                        .WithTargetFrameworks("netstandard1.1")
                        .Dir()
                        .WithFile("Class2.cs")
                    .Up()
                    .SubDir("Project1")
                        .WithCSharpProject("Project1", out var target)
                        .WithTargetFrameworks("netcoreapp1.0", "net451")
                        .WithProjectReference(proj2)
                        .Dir()
                        .WithFile("Class1.cs")
                    .Up()
                .Up()
                .Create();

            var fileset = await GetFileSet(target);

            AssertEx.EqualFileList(
                _tempDir.Root,
                new[]
                {
                    "src/Project2/Project2.csproj",
                    "src/Project2/Class2.cs",
                    "src/Project1/Project1.csproj",
                    "src/Project1/Class1.cs",
                },
                fileset
            );
        }

        [Fact]
        public async Task TransitiveProjectReferences_TwoLevels()
        {
            _tempDir
                .SubDir("src")
                    .SubDir("Project3")
                        .WithCSharpProject("Project3", out var proj3)
                        .WithTargetFrameworks("netstandard1.0")
                        .Dir()
                        .WithFile("Class3.cs")
                    .Up()
                    .SubDir("Project2")
                        .WithCSharpProject("Project2", out TemporaryCSharpProject proj2)
                        .WithTargetFrameworks("netstandard1.1")
                        .WithProjectReference(proj3)
                        .Dir()
                        .WithFile("Class2.cs")
                    .Up()
                    .SubDir("Project1")
                        .WithCSharpProject("Project1", out TemporaryCSharpProject target)
                        .WithTargetFrameworks("netcoreapp1.0", "net451")
                        .WithProjectReference(proj2)
                        .Dir()
                        .WithFile("Class1.cs");

            var fileset = await GetFileSet(target);

            AssertEx.EqualFileList(
                _tempDir.Root,
                new[]
                {
                    "src/Project3/Project3.csproj",
                    "src/Project3/Class3.cs",
                    "src/Project2/Project2.csproj",
                    "src/Project2/Class2.cs",
                    "src/Project1/Project1.csproj",
                    "src/Project1/Class1.cs",
                },
                fileset
            );
        }

        [Fact]
        public async Task ProjectReferences_Graph()
        {
            var graph = new TestProjectGraph(_tempDir);
            graph.OnCreate(p => p.WithTargetFrameworks("net45"));
            var matches = Regex.Matches(@"
            A->B B->C C->D D->E
                 B->E
            A->F F->G G->E
                 F->E
            W->U
            Y->Z
            Y->B
            Y->F",
            @"(\w)->(\w)");

            Assert.Equal(13, matches.Count);
            foreach (Match m in matches)
            {
                var target = graph.GetOrCreate(m.Groups[2].Value);
                graph.GetOrCreate(m.Groups[1].Value).WithProjectReference(target);
            }

            graph.Find("A").WithProjectReference(graph.Find("W"), watch: false);

            var output = new OutputSink();
            var filesetFactory = new MsBuildFileSetFactory(_reporter, graph.GetOrCreate("A").Path, output, trace: true);

            var fileset = await GetFileSet(filesetFactory);

            _reporter.Output(string.Join(
                Environment.NewLine,
                output.Current.Lines.Select(l => "Sink output: " + l)));

            var includedProjects = new[] { "A", "B", "C", "D", "E", "F", "G" };
            AssertEx.EqualFileList(
                _tempDir.Root,
                includedProjects
                    .Select(p => $"{p}/{p}.csproj"),
                fileset
            );

            // ensure unreachable projects exist but where not included
            Assert.NotNull(graph.Find("W"));
            Assert.NotNull(graph.Find("U"));
            Assert.NotNull(graph.Find("Y"));
            Assert.NotNull(graph.Find("Z"));

            // ensure each project is only visited once for collecting watch items
            Assert.All(includedProjects,
                projectName =>
                    Assert.Single(output.Current.Lines,
                        line => line.Contains($"Collecting watch items from '{projectName}'"))
            );
        }

        private Task<IFileSet> GetFileSet(TemporaryCSharpProject target)
            => GetFileSet(new MsBuildFileSetFactory(_reporter, target.Path, waitOnError: false, trace: false));

        private async Task<IFileSet> GetFileSet(MsBuildFileSetFactory filesetFactory)
        {
            _tempDir.Create();
            return await filesetFactory
                .CreateAsync(CancellationToken.None)
                .TimeoutAfter(TimeSpan.FromSeconds(30));
        }

        public void Dispose()
        {
            _tempDir.Dispose();
        }
    }
}
