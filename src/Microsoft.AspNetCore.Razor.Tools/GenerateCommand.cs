// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.LanguageServices.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Tools
{
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
            Version = Option("-v|--version", "Razor language version", CommandOptionType.SingleValue);
            Configuration = Option("-c", "Razor configuration name", CommandOptionType.SingleValue);
            ExtensionNames = Option("-n", "extension name", CommandOptionType.MultipleValue);
            ExtensionFilePaths = Option("-e", "extension file path", CommandOptionType.MultipleValue);
        }

        public CommandOption Sources { get; }

        public CommandOption Outputs { get; }

        public CommandOption RelativePaths { get; }

        public CommandOption ProjectDirectory { get; }

        public CommandOption TagHelperManifest { get; }

        public CommandOption Version { get; }

        public CommandOption Configuration { get; }

        public CommandOption ExtensionNames { get; }

        public CommandOption ExtensionFilePaths { get; }

        protected override Task<int> ExecuteCoreAsync()
        {
            // Loading all of the extensions should succeed as the dependency checker will have already
            // loaded them.
            var extensions = new RazorExtension[ExtensionNames.Values.Count];
            for (var i = 0; i < ExtensionNames.Values.Count; i++)
            {
                extensions[i] = new AssemblyExtension(ExtensionNames.Values[i], Parent.Loader.LoadFromPath(ExtensionFilePaths.Values[i]));
            }

            var version = RazorLanguageVersion.Parse(Version.Value());
            var configuration = RazorConfiguration.Create(version, Configuration.Value(), extensions);

            var result = ExecuteCore(
                configuration: configuration,
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
                Error.WriteLine($"{Sources.Description} should have at least one value.");
                return false;
            }

            if (Outputs.Values.Count != Sources.Values.Count)
            {
                Error.WriteLine($"{Sources.Description} has {Sources.Values.Count}, but {Outputs.Description} has {Outputs.Values.Count} values.");
            }

            if (RelativePaths.Values.Count != Sources.Values.Count)
            {
                Error.WriteLine($"{Sources.Description} has {Sources.Values.Count}, but {RelativePaths.Description} has {RelativePaths.Values.Count} values.");
            }

            if (string.IsNullOrEmpty(ProjectDirectory.Value()))
            {
                ProjectDirectory.Values.Add(Environment.CurrentDirectory);
            }

            if (string.IsNullOrEmpty(Version.Value()))
            {
                Error.WriteLine($"{Version.Description} must be specified.");
                return false;
            }
            else if (!RazorLanguageVersion.TryParse(Version.Value(), out _))
            {
                Error.WriteLine($"Invalid option {Version.Value()} for Razor language version --version; must be Latest or a valid version in range {RazorLanguageVersion.Version_1_0} to {RazorLanguageVersion.Latest}.");
                return false;
            }

            if (string.IsNullOrEmpty(Configuration.Value()))
            {
                Error.WriteLine($"{Configuration.Description} must be specified.");
                return false;
            }

            if (ExtensionNames.Values.Count != ExtensionFilePaths.Values.Count)
            {
                Error.WriteLine($"{ExtensionNames.Description} and {ExtensionFilePaths.Description} should have the same number of values.");
            }

            foreach (var filePath in ExtensionFilePaths.Values)
            {
                if (!Path.IsPathRooted(filePath))
                {
                    Error.WriteLine($"Extension file paths must be fully-qualified, absolute paths.");
                    return false;
                }
            }

            if (!Parent.Checker.Check(ExtensionFilePaths.Values))
            {
                Error.WriteLine($"Extensions could not be loaded. See output for details.");
                return false;
            }

            return true;
        }

        private int ExecuteCore(
            RazorConfiguration configuration,
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

            var engine = RazorProjectEngine.Create(configuration, compositeFileSystem, b =>
            {
                b.Features.Add(new StaticTagHelperFeature() { TagHelpers = tagHelpers, });
            });

            var results = GenerateCode(engine, inputItems);

            var success = true;

            foreach (var result in results)
            {
                var errorCount = result.CSharpDocument.Diagnostics.Count;
                if (errorCount > 0)
                {
                    success = false;

                    for (var i = 0; i < errorCount; i++)
                    {
                        var error = result.CSharpDocument.Diagnostics[i];
                        Error.WriteLine(error.ToString());

                        // Only show the first 100 errors to prevent massive string allocations.
                        if (i == 99)
                        {
                            Error.WriteLine($"And {errorCount - i + 1} more errors.");
                            break;
                        }
                    }
                }
                else
                {
                    // Only output the file if we generated it without errors.
                    var outputFilePath = result.InputItem.OutputPath;
                    File.WriteAllText(outputFilePath, result.CSharpDocument.GeneratedCode);
                }
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

        private OutputItem[] GenerateCode(RazorProjectEngine engine, SourceItem[] inputs)
        {
            var outputs = new OutputItem[inputs.Length];
            Parallel.For(0, outputs.Length, new ParallelOptions() { MaxDegreeOfParallelism = Debugger.IsAttached ? 1 : 4 }, i =>
            {
                var inputItem = inputs[i];

                var codeDocument = engine.Process(engine.FileSystem.GetItem(inputItem.FilePath));
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
