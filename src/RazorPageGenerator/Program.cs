// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.CodeGenerators;

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


            var viewDirectories = Directory.EnumerateDirectories(targetProjectDirectory, "Views", SearchOption.AllDirectories);

            var fileCount = 0;
            foreach (var viewDir in viewDirectories)
            {
                Console.WriteLine();
                Console.WriteLine("  Generating code files for views in {0}", viewDir);

                var cshtmlFiles = Directory.EnumerateFiles(viewDir, "*.cshtml");

                if (!cshtmlFiles.Any())
                {
                    Console.WriteLine("  No .cshtml files were found.");
                    continue;
                }

                foreach (var fileName in cshtmlFiles)
                {
                    Console.WriteLine("    Generating code file for view {0}...", Path.GetFileName(fileName));
                    GenerateCodeFile(fileName, rootNamespace);
                    Console.WriteLine("      Done!");
                    fileCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("{0} files successfully generated.", fileCount);
            Console.WriteLine();
        }

        private static void GenerateCodeFile(string cshtmlFilePath, string rootNamespace)
        {
            var basePath = Path.GetDirectoryName(cshtmlFilePath);
            var fileName = Path.GetFileName(cshtmlFilePath);
            var fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
            var codeLang = new CSharpRazorCodeLanguage();
            var host = new RazorEngineHost(codeLang);
            host.DefaultBaseClass = "Microsoft.Extensions.RazorViews.BaseView";
            host.GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: GeneratedClassContext.DefaultExecuteMethodName,
                writeMethodName: GeneratedClassContext.DefaultWriteMethodName,
                writeLiteralMethodName: GeneratedClassContext.DefaultWriteLiteralMethodName,
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: "HelperResult",
                defineSectionMethodName: "DefineSection",
                generatedTagHelperContext: new GeneratedTagHelperContext());
            var engine = new RazorTemplateEngine(host);

            var cshtmlContent = File.ReadAllText(cshtmlFilePath);
            cshtmlContent = ProcessFileIncludes(basePath, cshtmlContent);

            var generatorResults = engine.GenerateCode(
                    input: new StringReader(cshtmlContent),
                    className: fileNameNoExtension,
                    rootNamespace: Path.GetFileName(rootNamespace),
                    sourceFileName: fileName);

            var generatedCode = generatorResults.GeneratedCode;

            // Make the generated class 'internal' instead of 'public'
            generatedCode = generatedCode.Replace("public class", "internal class");

            File.WriteAllText(Path.Combine(basePath, string.Format("{0}.Designer.cs", fileNameNoExtension)), generatedCode);
        }

        private static string ProcessFileIncludes(string basePath, string cshtmlContent)
        {
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
                    throw new InvalidOperationException("Invalid include file format. Usage example: <%$ include: ErrorPage.js %>");
                }
                var includeFileName = cshtmlContent.Substring(startIndex + startMatch.Length, endIndex - (startIndex + startMatch.Length));
                Console.WriteLine("      Inlining file {0}", includeFileName);
                var includeFileContent = File.ReadAllText(Path.Combine(basePath, includeFileName));
                cshtmlContent = cshtmlContent.Substring(0, startIndex) + includeFileContent + cshtmlContent.Substring(endIndex + endMatch.Length);
                startIndex = startIndex + includeFileContent.Length;
            }
            return cshtmlContent;
        }
    }
}
