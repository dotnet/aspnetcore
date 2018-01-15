// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class GenerateCommand : CommandBase
    {
        public GenerateCommand(Application parent)
            : base(parent, "generate")
        {
            Sources = Argument("sources", ".cshtml files to compile", multipleValues: true);
            ProjectDirectory = Option("-p", "project root directory", CommandOptionType.SingleValue);
            OutputDirectory = Option("-o", "output directory", CommandOptionType.SingleValue);
            TagHelperManifest = Option("-t", "tag helper manifest file", CommandOptionType.SingleValue);
        }

        public CommandArgument Sources { get; }

        public CommandOption OutputDirectory { get; }

        public CommandOption ProjectDirectory { get; }

        public CommandOption TagHelperManifest { get; }

        protected override Task<int> ExecuteCoreAsync()
        {
            var result = ExecuteCore(
                projectDirectory: ProjectDirectory.Value() ?? Environment.CurrentDirectory,
                outputDirectory: OutputDirectory.Value(),
                tagHelperManifest: TagHelperManifest.Value(),
                sources: Sources.Values.ToArray());

            return Task.FromResult(result);
        }

        protected override bool ValidateArguments()
        {
            if (string.IsNullOrEmpty(OutputDirectory.Value()))
            {
                Error.WriteLine($"{OutputDirectory.ValueName} not specified.");
                return false;
            }

            if (Sources.Values.Count == 0)
            {
                Error.WriteLine($"{Sources.Name} should have at least one value.");
                return false;
            }

            return true;
        }

        private int ExecuteCore(string projectDirectory, string outputDirectory, string tagHelperManifest, string[] sources)
        {
            tagHelperManifest = Path.Combine(projectDirectory, tagHelperManifest);
            outputDirectory = Path.Combine(projectDirectory, outputDirectory);

            var tagHelpers = GetTagHelpers(tagHelperManifest);

            var engine = RazorEngine.Create(b =>
            {
                RazorExtensions.Register(b);

                b.Features.Add(new StaticTagHelperFeature() { TagHelpers = tagHelpers, });
            });

            var templateEngine = new MvcRazorTemplateEngine(engine, RazorProject.Create(projectDirectory));

            var sourceItems = GetRazorFiles(projectDirectory, sources);
            var results = GenerateCode(templateEngine, sourceItems);

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

                var outputFilePath = Path.Combine(outputDirectory, Path.ChangeExtension(result.ViewFileInfo.ViewEnginePath.Substring(1), ".cs"));
                File.WriteAllText(outputFilePath, result.CSharpDocument.GeneratedCode);
            }

            return success ? 0 : -1;
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

                return serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }
        }

        private List<SourceItem> GetRazorFiles(string projectDirectory, string[] sources)
        {
            var trimLength = projectDirectory.EndsWith("/") ? projectDirectory.Length - 1 : projectDirectory.Length;

            var items = new List<SourceItem>(sources.Length);
            for (var i = 0; i < sources.Length; i++)
            {
                var fullPath = Path.Combine(projectDirectory, sources[i]);
                if (fullPath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    var viewEnginePath = fullPath.Substring(trimLength).Replace('\\', '/');
                    items.Add(new SourceItem(fullPath, viewEnginePath));
                }
            }

            return items;
        }

        private OutputItem[] GenerateCode(RazorTemplateEngine templateEngine, IReadOnlyList<SourceItem> sources)
        {
            var outputs = new OutputItem[sources.Count];
            Parallel.For(0, outputs.Length, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, i =>
            {
                var source = sources[i];

                var csharpDocument = templateEngine.GenerateCode(source.ViewEnginePath);
                outputs[i] = new OutputItem(source, csharpDocument);
            });

            return outputs;
        }

        private struct OutputItem
        {
            public OutputItem(
                SourceItem viewFileInfo,
                RazorCSharpDocument cSharpDocument)
            {
                ViewFileInfo = viewFileInfo;
                CSharpDocument = cSharpDocument;
            }

            public SourceItem ViewFileInfo { get; }

            public RazorCSharpDocument CSharpDocument { get; }
        }

        private struct SourceItem
        {
            public SourceItem(string fullPath, string viewEnginePath)
            {
                FullPath = fullPath;
                ViewEnginePath = viewEnginePath;
            }

            public string FullPath { get; }

            public string ViewEnginePath { get; }

            public Stream CreateReadStream()
            {
                // We are setting buffer size to 1 to prevent FileStream from allocating it's internal buffer
                // 0 causes constructor to throw
                var bufferSize = 1;
                return new FileStream(
                    FullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
            }
        }

        private class StaticTagHelperFeature : ITagHelperFeature
        {
            public RazorEngine Engine { get; set; }

            public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors() => TagHelpers;
        }
    }
}