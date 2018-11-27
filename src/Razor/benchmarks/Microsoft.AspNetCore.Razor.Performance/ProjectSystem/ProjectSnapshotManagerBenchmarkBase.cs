// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Performance
{
    public class ProjectSnapshotManagerBenchmarkBase
    {
        public ProjectSnapshotManagerBenchmarkBase()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "Razor.sln")))
            {
                current = current.Parent;
            }

            var root = current;
            var projectRoot = Path.Combine(root.FullName, "test", "testapps", "LargeProject");

            HostProject = new HostProject(Path.Combine(projectRoot, "LargeProject.csproj"), FallbackRazorConfiguration.MVC_2_1);

            TextLoaders = new TextLoader[4];
            for (var i = 0; i < 4; i++)
            {
                var filePath = Path.Combine(projectRoot, "Views", "Home", $"View00{i % 4}.cshtml");
                var text = SourceText.From(filePath, encoding: null);
                TextLoaders[i] = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()));
            }

            Documents = new HostDocument[100];
            for (var i = 0; i < Documents.Length; i++)
            {
                var filePath = Path.Combine(projectRoot, "Views", "Home", $"View00{i % 4}.cshtml");
                Documents[i] = new HostDocument(filePath, $"/Views/Home/View00{i}.cshtml");
            }

            var tagHelpers = Path.Combine(root.FullName, "benchmarks", "Microsoft.AspNetCore.Razor.Performance", "taghelpers.json");
            TagHelperResolver = new StaticTagHelperResolver(ReadTagHelpers(tagHelpers));
        }

        internal HostProject HostProject { get; }

        internal HostDocument[] Documents { get; }

        internal TextLoader[] TextLoaders { get; }

        internal TagHelperResolver TagHelperResolver { get; }

        internal DefaultProjectSnapshotManager CreateProjectSnapshotManager()
        {
            var services = TestServices.Create(
                new IWorkspaceService[]
                {
                    new StaticProjectSnapshotProjectEngineFactory(),
                },
                new ILanguageService[]
                {
                    TagHelperResolver,
                });

            return new DefaultProjectSnapshotManager(
            new TestForegroundDispatcher(),
            new TestErrorReporter(),
            Array.Empty<ProjectSnapshotChangeTrigger>(),
            new AdhocWorkspace(services));
        }

        private static IReadOnlyList<TagHelperDescriptor> ReadTagHelpers(string filePath)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new RazorDiagnosticJsonConverter());
            serializer.Converters.Add(new TagHelperDescriptorJsonConverter());

            using (var reader = new JsonTextReader(File.OpenText(filePath)))
            {
                return serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }
        }

        private class TestForegroundDispatcher : ForegroundDispatcher
        {
            public override bool IsForegroundThread => true;

            public override TaskScheduler ForegroundScheduler => TaskScheduler.Default;

            public override TaskScheduler BackgroundScheduler => TaskScheduler.Default;
        }

        private class TestErrorReporter : ErrorReporter
        {
            public override void ReportError(Exception exception)
            {
            }

            public override void ReportError(Exception exception, ProjectSnapshot project)
            {
            }

            public override void ReportError(Exception exception, Project workspaceProject)
            {
            }
        }

        private class StaticTagHelperResolver : TagHelperResolver
        {
            private readonly IReadOnlyList<TagHelperDescriptor> _tagHelpers;

            public StaticTagHelperResolver(IReadOnlyList<TagHelperDescriptor> tagHelpers)
            {
                this._tagHelpers = tagHelpers;
            }

            public override Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new TagHelperResolutionResult(_tagHelpers, Array.Empty<RazorDiagnostic>()));
            }
        }

        private class StaticProjectSnapshotProjectEngineFactory : ProjectSnapshotProjectEngineFactory
        {
            public override IProjectEngineFactory FindFactory(ProjectSnapshot project)
            {
                throw new NotImplementedException();
            }

            public override IProjectEngineFactory FindSerializableFactory(ProjectSnapshot project)
            {
                throw new NotImplementedException();
            }

            public override RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
            {
                return RazorProjectEngine.Create(configuration, fileSystem, b =>
                {
                    RazorExtensions.Register(b);
                });
            }
        }
    }
}
