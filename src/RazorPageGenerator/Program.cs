// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace RazorPageGenerator
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.WriteLine("Invalid argument(s).");
                Console.WriteLine(@"Usage:   
    dotnet razorpagegenerator <root-namespace-of-views> [path]
Examples: 
    dotnet razorpagegenerator Microsoft.AspNetCore.Diagnostics.RazorViews
        - processes all views in ""Views"" subfolders of the current directory
    dotnet razorpagegenerator Microsoft.AspNetCore.Diagnostics.RazorViews c:\project
        - processes all views in ""Views"" subfolders of c:\project directory
");

                return 1;
            }

            var rootNamespace = args[0];
            var targetProjectDirectory = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
            var projectEngine = CreateProjectEngine(rootNamespace, targetProjectDirectory);
            var results = MainCore(projectEngine, targetProjectDirectory);

            foreach (var result in results)
            {
                File.WriteAllText(result.FilePath, result.GeneratedCode);
            }

            Console.WriteLine();
            Console.WriteLine($"{results.Count} files successfully generated.");
            Console.WriteLine();
            return 0;
        }

        public static RazorProjectEngine CreateProjectEngine(string rootNamespace, string targetProjectDirectory, Action<RazorProjectEngineBuilder> configure = null)
        {
            var fileSystem = RazorProjectFileSystem.Create(targetProjectDirectory);
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, builder =>
            {
                builder
                    .SetNamespace(rootNamespace)
                    .SetBaseType("Microsoft.Extensions.RazorViews.BaseView")
                    .ConfigureClass((document, @class) =>
                    {
                        @class.ClassName = Path.GetFileNameWithoutExtension(document.Source.FilePath);
                        @class.Modifiers.Clear();
                        @class.Modifiers.Add("internal");
                    });

                builder.Features.Add(new SuppressChecksumOptionsFeature());
                builder.Features.Add(new SuppressMetadataAttributesFeature());

                if (configure != null)
                {
                    configure(builder);
                }

                builder.AddDefaultImports(@"
@using System
@using System.Threading.Tasks
");
            });
            return projectEngine;
        }

        public static IList<RazorPageGeneratorResult> MainCore(RazorProjectEngine projectEngine, string targetProjectDirectory)
        {
            var viewDirectories = Directory.EnumerateDirectories(targetProjectDirectory, "Views", SearchOption.AllDirectories);
            var fileCount = 0;

            var results = new List<RazorPageGeneratorResult>();
            foreach (var viewDir in viewDirectories)
            {
                Console.WriteLine();
                Console.WriteLine("  Generating code files for views in {0}", viewDir);
                var viewDirPath = viewDir.Substring(targetProjectDirectory.Length).Replace('\\', '/');
                var cshtmlFiles = projectEngine.FileSystem.EnumerateItems(viewDirPath);

                if (!cshtmlFiles.Any())
                {
                    Console.WriteLine("  No .cshtml files were found.");
                    continue;
                }

                foreach (var item in cshtmlFiles)
                {
                    Console.WriteLine("    Generating code file for view {0}...", item.FileName);
                    results.Add(GenerateCodeFile(projectEngine, item));
                    Console.WriteLine("      Done!");
                    fileCount++;
                }
            }

            return results;
        }

        private static RazorPageGeneratorResult GenerateCodeFile(RazorProjectEngine projectEngine, RazorProjectItem projectItem)
        {
            var projectItemWrapper = new FileSystemRazorProjectItemWrapper(projectItem);
            var codeDocument = projectEngine.Process(projectItemWrapper);
            var cSharpDocument = codeDocument.GetCSharpDocument();
            if (cSharpDocument.Diagnostics.Any())
            {
                var diagnostics = string.Join(Environment.NewLine, cSharpDocument.Diagnostics);
                Console.WriteLine($"One or more parse errors encountered. This will not prevent the generator from continuing: {Environment.NewLine}{diagnostics}.");
            }

            var generatedCodeFilePath = Path.ChangeExtension(projectItem.PhysicalPath, ".Designer.cs");
            return new RazorPageGeneratorResult
            {
                FilePath = generatedCodeFilePath,
                GeneratedCode = cSharpDocument.GeneratedCode,
            };
        }

        private class SuppressChecksumOptionsFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                options.SuppressChecksum = true;
            }
        }

        private class SuppressMetadataAttributesFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                options.SuppressMetadataAttributes = true;
            }
        }

        private class FileSystemRazorProjectItemWrapper : RazorProjectItem
        {
            private readonly RazorProjectItem _source;

            public FileSystemRazorProjectItemWrapper(RazorProjectItem item)
            {
                _source = item;
            }

            public override string BasePath => _source.BasePath;

            public override string FilePath => _source.FilePath;

            // Mask the full name since we don't want a developer's local file paths to be commited.
            public override string PhysicalPath => _source.FileName;

            public override bool Exists => _source.Exists;

            public override Stream Read()
            {
                var processedContent = ProcessFileIncludes();
                return new MemoryStream(Encoding.UTF8.GetBytes(processedContent));
            }

            private string ProcessFileIncludes()
            {
                var basePath = System.IO.Path.GetDirectoryName(_source.PhysicalPath);
                var cshtmlContent = File.ReadAllText(_source.PhysicalPath);

                var startMatch = "<%$ include: ";
                var endMatch = " %>";
                var startIndex = 0;
                while (startIndex < cshtmlContent.Length)
                {
                    startIndex = cshtmlContent.IndexOf(startMatch, startIndex);
                    if (startIndex == -1)
                    {
                        break;
                    }
                    var endIndex = cshtmlContent.IndexOf(endMatch, startIndex);
                    if (endIndex == -1)
                    {
                        throw new InvalidOperationException($"Invalid include file format in {_source.PhysicalPath}. Usage example: <%$ include: ErrorPage.js %>");
                    }
                    var includeFileName = cshtmlContent.Substring(startIndex + startMatch.Length, endIndex - (startIndex + startMatch.Length));
                    Console.WriteLine("      Inlining file {0}", includeFileName);
                    var includeFileContent = File.ReadAllText(System.IO.Path.Combine(basePath, includeFileName));
                    cshtmlContent = cshtmlContent.Substring(0, startIndex) + includeFileContent + cshtmlContent.Substring(endIndex + endMatch.Length);
                    startIndex = startIndex + includeFileContent.Length;
                }
                return cshtmlContent;
            }
        }
    }
}
