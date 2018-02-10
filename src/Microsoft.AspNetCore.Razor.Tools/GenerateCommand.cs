// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class Builder<T>
    {
        public static Builder<T> Make(CommandBase result) => null;

        public static Builder<T> Make(T result) => null;
    }

    internal class GenerateCommand : CommandBase
    {
        public GenerateCommand(Application parent)
            : base(parent, "generate")
        {
            Sources = Option("-s", ".cshtml files to compile", CommandOptionType.MultipleValue);
            Outputs = Option("-o", "Generated output file path", CommandOptionType.MultipleValue);
            RelativePaths = Option("-r", "Relative path", CommandOptionType.MultipleValue);
            ProjectDirectory = Option("-p", "project root directory", CommandOptionType.SingleValue);
            TagHelperManifest = Option("-t", "tag helper manifest file", CommandOptionType.SingleValue);
        }

        public CommandOption Sources { get; }

        public CommandOption Outputs { get; }

        public CommandOption RelativePaths { get; }

        public CommandOption ProjectDirectory { get; }

        public CommandOption TagHelperManifest { get; }

        protected override Task<int> ExecuteCoreAsync()
        {
            var result = ExecuteCore(
                projectDirectory: ProjectDirectory.Value(),
                tagHelperManifest: TagHelperManifest.Value(),
                sources: Sources.Values,
                outputs: Outputs.Values,
                relativePaths: RelativePaths.Values);

            return Task.FromResult(result);
        }

        protected override bool ValidateArguments()
        {
            if (Sources.Values.Count == 0)
            {
                Error.WriteLine($"{Sources.ValueName} should have at least one value.");
                return false;
            }

            if (Outputs.Values.Count != Sources.Values.Count)
            {
                Error.WriteLine($"{Sources.ValueName} has {Sources.Values.Count}, but {Outputs.ValueName} has {Outputs.Values.Count}.");
            }

            if (RelativePaths.Values.Count != Sources.Values.Count)
            {
                Error.WriteLine($"{Sources.ValueName} has {Sources.Values.Count}, but {RelativePaths.ValueName} has {RelativePaths.Values.Count}.");
            }

            if (string.IsNullOrEmpty(ProjectDirectory.Value()))
            {
                ProjectDirectory.Values.Add(Environment.CurrentDirectory);
            }

            return true;
        }

        private int ExecuteCore(
            string projectDirectory,
            string tagHelperManifest,
            List<string> sources,
            List<string> outputs,
            List<string> relativePaths)
        {
            tagHelperManifest = Path.Combine(projectDirectory, tagHelperManifest);

            var tagHelpers = GetTagHelpers(tagHelperManifest);
            var inputItems = GetInputItems(projectDirectory, sources, outputs, relativePaths);
            var compositeFileSystem = new CompositeRazorProjectFileSystem(new[]
            {
                GetVirtualRazorProjectSystem(inputItems),
                RazorProjectFileSystem.Create(projectDirectory),
            });
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, compositeFileSystem, b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(new StaticTagHelperFeature() { TagHelpers = tagHelpers, });
            });

            var results = GenerateCode(projectEngine, inputItems);

            var success = true;

            foreach (var result in results)
            {
                if (result.CSharpDocument.Diagnostics.Count > 0)
                {
                    success = false;
                    foreach (var error in result.CSharpDocument.Diagnostics)
                    {
                        Console.Error.WriteLine(error.ToString());
                    }
                }

                var outputFilePath = result.InputItem.OutputPath;
                File.WriteAllText(outputFilePath, result.CSharpDocument.GeneratedCode);
            }

            return success ? 0 : -1;
        }

        private VirtualRazorProjectFileSystem GetVirtualRazorProjectSystem(SourceItem[] inputItems)
        {
            var project = new VirtualRazorProjectFileSystem();
            foreach (var item in inputItems)
            {
                var projectItem = new DefaultRazorProjectItem(
                    basePath: "/",
                    filePath: item.FilePath,
                    relativePhysicalPath: item.RelativePhysicalPath,
                    file: new FileInfo(item.SourcePath));

                project.Add(projectItem);
            }

            return project;
        }

        private IReadOnlyList<TagHelperDescriptor> GetTagHelpers(string tagHelperManifest)
        {
            if (!File.Exists(tagHelperManifest))
            {
                return Array.Empty<TagHelperDescriptor>();
            }

            using (var stream = File.OpenRead(tagHelperManifest))
            {
                var reader = new JsonTextReader(new StreamReader(stream));

                var serializer = new JsonSerializer();
                serializer.Converters.Add(new RazorDiagnosticJsonConverter());
                serializer.Converters.Add(new TagHelperDescriptorJsonConverter());

                var descriptors = serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
                return descriptors;
            }
        }

        private SourceItem[] GetInputItems(string projectDirectory, List<string> sources, List<string> outputs, List<string> relativePath)
        {
            var items = new SourceItem[sources.Count];
            for (var i = 0; i < items.Length; i++)
            {
                var outputPath = Path.Combine(projectDirectory, outputs[i]);
                items[i] = new SourceItem(sources[i], outputs[i], relativePath[i]);
            }

            return items;
        }

        private OutputItem[] GenerateCode(RazorProjectEngine projectEngine, SourceItem[] inputs)
        {
            var outputs = new OutputItem[inputs.Length];
            Parallel.For(0, outputs.Length, new ParallelOptions() { MaxDegreeOfParallelism = Debugger.IsAttached ? 1 : 4 }, i =>
            {
                var inputItem = inputs[i];
                var projectItem = projectEngine.FileSystem.GetItem(inputItem.FilePath);
                var codeDocument = projectEngine.Process(projectItem);
                var csharpDocument = codeDocument.GetCSharpDocument();
                outputs[i] = new OutputItem(inputItem, csharpDocument);
            });

            return outputs;
        }

        private struct OutputItem
        {
            public OutputItem(
                SourceItem inputItem,
                RazorCSharpDocument cSharpDocument)
            {
                InputItem = inputItem;
                CSharpDocument = cSharpDocument;
            }

            public SourceItem InputItem { get; }

            public RazorCSharpDocument CSharpDocument { get; }
        }

        private struct SourceItem
        {
            public SourceItem(string sourcePath, string outputPath, string physicalRelativePath)
            {
                SourcePath = sourcePath;
                OutputPath = outputPath;
                RelativePhysicalPath = physicalRelativePath;
                FilePath = '/' + physicalRelativePath
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Replace("//", "/");
            }

            public string SourcePath { get; }

            public string OutputPath { get; }

            public string RelativePhysicalPath { get; }

            public string FilePath { get; }
        }

        private class StaticTagHelperFeature : ITagHelperFeature
        {
            public RazorEngine Engine { get; set; }

            public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors() => TagHelpers;
        }
    }
}
