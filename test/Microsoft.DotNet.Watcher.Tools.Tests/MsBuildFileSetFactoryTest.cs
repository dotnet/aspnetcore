// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNetWatcher.Tools.Tests
{
    using ItemSpec = TemporaryCSharpProject.ItemSpec;

    public class MsBuildFileSetFactoryTest : IDisposable
    {
        private ILogger _logger;
        private readonly TemporaryDirectory _tempDir;
        public MsBuildFileSetFactoryTest(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output);
            _tempDir = new TemporaryDirectory();
        }

        [Fact]
        public async Task FindsCustomWatchItems()
        {
            TemporaryCSharpProject target;
            _tempDir
                .WithCSharpProject("Project1", out target)
                    .WithTargetFrameworks("netcoreapp1.0")
                    .WithDefaultGlobs()
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
            TemporaryCSharpProject target;
            _tempDir
                .WithCSharpProject("Project1", out target)
                    .WithTargetFrameworks("net40")
                    .WithItem(new ItemSpec { Name = "Compile", Include = "*.cs" })
                    .WithItem(new ItemSpec { Name = "EmbeddedResource", Include = "*.resx", Watch = false })
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
            TemporaryCSharpProject target;

            _tempDir
                .SubDir("src")
                    .SubDir("Project1")
                        .WithCSharpProject("Project1", out target)
                        .WithTargetFrameworks("netcoreapp1.0")
                        .WithDefaultGlobs()
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
            TemporaryCSharpProject target;
            _tempDir
                .SubDir("src")
                    .SubDir("Project1")
                        .WithCSharpProject("Project1", out target)
                        .WithTargetFrameworks("netcoreapp1.0", "net451")
                        .WithItem("Compile", "Class1.netcore.cs", "'$(TargetFramework)'=='netcoreapp1.0'")
                        .WithItem("Compile", "Class1.desktop.cs", "'$(TargetFramework)'=='net451'")
                        .Dir()
                        .WithFile("Class1.netcore.cs")
                        .WithFile("Class1.desktop.cs")
                        .WithFile("Class1.notincluded.cs");

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
            TemporaryCSharpProject target;
            TemporaryCSharpProject proj2;
            _tempDir
                .SubDir("src")
                    .SubDir("Project2")
                        .WithCSharpProject("Project2", out proj2)
                        .WithTargetFrameworks("netstandard1.1")
                        .WithDefaultGlobs()
                        .Dir()
                        .WithFile("Class2.cs")
                    .Up()
                    .SubDir("Project1")
                        .WithCSharpProject("Project1", out target)
                        .WithTargetFrameworks("netcoreapp1.0", "net451")
                        .WithProjectReference(proj2)
                        .WithDefaultGlobs()
                        .Dir()
                        .WithFile("Class1.cs");

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
            TemporaryCSharpProject target;
            TemporaryCSharpProject proj2;
            TemporaryCSharpProject proj3;
            _tempDir
                .SubDir("src")
                    .SubDir("Project3")
                        .WithCSharpProject("Project3", out proj3)
                        .WithTargetFrameworks("netstandard1.0")
                        .WithDefaultGlobs()
                        .Dir()
                        .WithFile("Class3.cs")
                    .Up()
                    .SubDir("Project2")
                        .WithCSharpProject("Project2", out proj2)
                        .WithTargetFrameworks("netstandard1.1")
                        .WithProjectReference(proj3)
                        .WithDefaultGlobs()
                        .Dir()
                        .WithFile("Class2.cs")
                    .Up()
                    .SubDir("Project1")
                        .WithCSharpProject("Project1", out target)
                        .WithTargetFrameworks("netcoreapp1.0", "net451")
                        .WithProjectReference(proj2)
                        .WithDefaultGlobs()
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
            graph.OnCreate(p => p.WithTargetFrameworks("net45").WithDefaultGlobs());
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
            var filesetFactory = new MsBuildFileSetFactory(_logger, graph.GetOrCreate("A").Path, output)
            {
                // enables capturing markers to know which projects have been visited
                BuildFlags = { "/p:_DotNetWatchTraceOutput=true" }
            };

            var fileset = await GetFileSet(filesetFactory);

            _logger.LogInformation(output.Current.GetAllLines("Sink output: "));

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

            // ensure each project is only visited once to collect project references
            Assert.All(includedProjects,
                projectName =>
                    Assert.Single(output.Current.Lines,
                    line => line.Contains($"Collecting referenced projects from '{projectName}'"))
            );
        }

        private Task<IFileSet> GetFileSet(TemporaryCSharpProject target)
            => GetFileSet(new MsBuildFileSetFactory(_logger, target.Path));
        private async Task<IFileSet> GetFileSet(MsBuildFileSetFactory filesetFactory)
        {
            _tempDir.Create();
            var createTask = filesetFactory.CreateAsync(CancellationToken.None);
            var finished = await Task.WhenAny(createTask, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.Same(createTask, finished);
            return createTask.Result;
        }

        public void Dispose()
        {
            _tempDir.Dispose();
        }
    }
}