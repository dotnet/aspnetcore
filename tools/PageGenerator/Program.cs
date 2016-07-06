// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.CodeGenerators;

namespace PageGenerator
{
    public class Program
    {
        private const int NumArgs = 1;

        public static void Main(string[] args)
        {
            if (args.Length != NumArgs)
            {
                throw new ArgumentException(string.Format("Requires {0} argument (Project Directory), {1} given", NumArgs, args.Length));
            }
            var diagnosticsDir = args[0];

            var viewDirectories = Directory.EnumerateDirectories(diagnosticsDir, "Views", SearchOption.AllDirectories);

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
                    var rootNamespace = Path.GetDirectoryName(diagnosticsDir);
                    GenerateCodeFile(fileName, $"{rootNamespace}.Views");
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
            host.DefaultBaseClass = "Microsoft.AspNetCore.DiagnosticsViewPage.Views.BaseView";
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
                    rootNamespace: Path.GetFileName(rootNamespace),
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
                    Console.WriteLine("      Inlining file {0}", includeFileName);
                    var replacement = File.ReadAllText(Path.Combine(basePath, includeFileName)).Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
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
            //These are already in the list on windows, but for other platforms
            //it seems like some of them are missing, so we add them explicitly
            invalidChars.Add('"');
            invalidChars.Add('\'');
            invalidChars.Add('\r');
            invalidChars.Add('\n');

            return string.Join(string.Empty, fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        }
    }
}
