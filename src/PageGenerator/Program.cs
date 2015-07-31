// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.Dnx.Runtime;

namespace PageGenerator
{
    public class Program
    {
        private const int NumArgs = 1;

        private readonly ILibraryManager _libraryManager;

        public Program(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public void Main(string[] args)
        {
            if (args.Length != NumArgs)
            {
                throw new ArgumentException(string.Format("Requires {0} argument (Library Name), {1} given", NumArgs, args.Length));
            }
            var diagnosticsLibInfo = _libraryManager.GetLibrary(args[0]);
            if (diagnosticsLibInfo == null)
            {
                throw new ArgumentException(string.Format(
                    "Unable to open library {0}. Is it spelled correctly and listed as a dependency in project.json?",
                    args[0]));
            }
            var viewBasePath = Path.Combine(Path.GetDirectoryName(diagnosticsLibInfo.Path), "Views");

            Console.WriteLine("Generating code files for views in {0}", viewBasePath);
            Console.WriteLine();

            var cshtmlFiles = GetCshtmlFiles(viewBasePath);

            var fileCount = 0;
            foreach (var fileName in cshtmlFiles)
            {
                Console.WriteLine("  Generating code file for view {0}...", Path.GetFileName(fileName));
                GenerateCodeFile(fileName, string.Format("{0}.Views", args[0]));
                Console.WriteLine("      Done!");
                fileCount++;
            }

            Console.WriteLine();
            Console.WriteLine("{0} files successfully generated.", fileCount);
            Console.WriteLine();
        }

        private static IEnumerable<string> GetCshtmlFiles(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new ArgumentException("path");
            }

            return Directory.EnumerateFiles(path, "*.cshtml");
        }

        private static void GenerateCodeFile(string cshtmlFilePath, string rootNamespace)
        {
            var basePath = Path.GetDirectoryName(cshtmlFilePath);
            var fileName = Path.GetFileName(cshtmlFilePath);
            var fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
            var codeLang = new CSharpRazorCodeLanguage();
            var host = new RazorEngineHost(codeLang);
            host.DefaultBaseClass = "Microsoft.AspNet.Diagnostics.Views.BaseView";
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

            using (var fileStream = File.OpenText(cshtmlFilePath))
            {
                var code = engine.GenerateCode(
                    input: fileStream,
                    className: fileNameNoExtension,
                    rootNamespace: rootNamespace,
                    sourceFileName: fileName);

                var source = code.GeneratedCode;
                var startIndex = 0;
                while (startIndex < source.Length)
                {
                    var startMatch = @"<%$ include: ";
                    var endMatch = @" %>";
                    startIndex = source.IndexOf(startMatch, startIndex);
                    if (startIndex == -1)
                    {
                        break;
                    }
                    var endIndex = source.IndexOf(endMatch, startIndex);
                    if (endIndex == -1)
                    {
                        break;
                    }
                    var includeFileName = source.Substring(startIndex + startMatch.Length, endIndex - (startIndex + startMatch.Length));
                    includeFileName = SanitizeFileName(includeFileName);
                    Console.WriteLine("    Inlining file {0}", includeFileName);
                    var replacement = File.ReadAllText(Path.Combine(basePath, includeFileName)).Replace("\"", "\\\"").Replace("\r\n", "\\r\\n");
                    source = source.Substring(0, startIndex) + replacement + source.Substring(endIndex + endMatch.Length);
                    startIndex = startIndex + replacement.Length;
                }
                File.WriteAllText(Path.Combine(basePath, string.Format("{0}.cs", fileNameNoExtension)), source);
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            // The Razor generated code sometimes splits strings across multiple lines
            // which can hit the include file name, so we need to strip out the non-filename chars.
            //ErrorPage.j" +
            //"s

            var invalidChars = new List<char>(Path.GetInvalidFileNameChars());
            invalidChars.Add('+');
            invalidChars.Add(' ');

            return string.Join(string.Empty, fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        }
    }
}
