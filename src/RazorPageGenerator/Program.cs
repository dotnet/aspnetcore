// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Evolution;

namespace RazorPageGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length != 1)
            {
                Console.WriteLine("Invalid argument(s).");
                Console.WriteLine("Usage:   dotnet razorpagegenerator <root-namespace-of-views>");
                Console.WriteLine("Example: dotnet razorpagegenerator Microsoft.AspNetCore.Diagnostics.RazorViews");
                return;
            }

            var rootNamespace = args[0];
            var targetProjectDirectory = Directory.GetCurrentDirectory();
            var razorEngine = RazorEngine.Create(builder =>
            {
                builder
                    .SetNamespace(rootNamespace)
                    .SetBaseType("Microsoft.Extensions.RazorViews.BaseView")
                    .ConfigureClass((document, @class) =>
                    {
                        @class.Name = Path.GetFileNameWithoutExtension(document.Source.Filename);
                        @class.AccessModifier = "internal";
                    });

                builder.Features.Add(new RemovePragamaChecksumFeature());

            });

            var viewDirectories = Directory.EnumerateDirectories(targetProjectDirectory, "Views", SearchOption.AllDirectories);

            var fileCount = 0;
            foreach (var viewDir in viewDirectories)
            {
                Console.WriteLine();
                Console.WriteLine("  Generating code files for views in {0}", viewDir);
                var razorProject = new FileSystemRazorProject(viewDir);
                var templateEngine = new RazorTemplateEngine(razorEngine, razorProject);


                var cshtmlFiles = razorProject.EnumerateItems("");

                if (!cshtmlFiles.Any())
                {
                    Console.WriteLine("  No .cshtml files were found.");
                    continue;
                }

                foreach (var item in cshtmlFiles)
                {
                    Console.WriteLine("    Generating code file for view {0}...", item.Filename);
                    GenerateCodeFile(templateEngine, item);
                    Console.WriteLine("      Done!");
                    fileCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("{0} files successfully generated.", fileCount);
            Console.WriteLine();
        }

        private static void GenerateCodeFile(RazorTemplateEngine templateEngine, RazorProjectItem projectItem)
        {
            var cSharpDocument = templateEngine.GenerateCode(projectItem);
            if (cSharpDocument.Diagnostics.Any())
            {
                var diagnostics = string.Join(Environment.NewLine, cSharpDocument.Diagnostics);
                Console.WriteLine($"One or more parse errors encountered. This will not prevent the generator from continuing: {Environment.NewLine}{diagnostics}.");
            }

            var generatedCodeFilePath = Path.ChangeExtension(
                ((FileSystemRazorProjectItem)projectItem).FileInfo.FullName,
                ".Designer.cs");
            File.WriteAllText(generatedCodeFilePath, cSharpDocument.GeneratedCode);
        }

        private class FileSystemRazorProject : RazorProject
        {
            private readonly string _basePath;

            public FileSystemRazorProject(string basePath)
            {
                _basePath = basePath;
            }

            public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
            {
                return new DirectoryInfo(_basePath)
                    .EnumerateFiles("*.cshtml", SearchOption.TopDirectoryOnly)
                    .Select(file => GetItem(basePath, file));
            }

            public override RazorProjectItem GetItem(string path) => throw new NotSupportedException();

            private RazorProjectItem GetItem(string basePath, FileInfo file)
            {
                if (!file.Exists)
                {
                    throw new FileNotFoundException($"{file.FullName} does not exist.");
                }

                return new FileSystemRazorProjectItem(basePath, file);
            }
        }

        private class FileSystemRazorProjectItem : RazorProjectItem
        {
            public FileSystemRazorProjectItem(string basePath, FileInfo fileInfo)
            {
                BasePath = basePath;
                Path = fileInfo.Name;
                FileInfo = fileInfo;
            }

            public FileInfo FileInfo { get; }

            public override string BasePath { get; }

            public override string Path { get; }

            // Mask the full name since we don't want a developer's local file paths to be commited.
            public override string PhysicalPath => FileInfo.Name;

            public override bool Exists => true;

            public override Stream Read()
            {
                var processedContent = ProcessFileIncludes();
                return new MemoryStream(Encoding.UTF8.GetBytes(processedContent));
            }

            private string ProcessFileIncludes()
            {
                var basePath = FileInfo.DirectoryName;
                var cshtmlContent = File.ReadAllText(FileInfo.FullName);

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
                        throw new InvalidOperationException($"Invalid include file format in {FileInfo.FullName}. Usage example: <%$ include: ErrorPage.js %>");
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
