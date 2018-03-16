// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    internal class PrecompileRunCommand
    {
        private static readonly ParallelOptions ParalellOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4
        };

        private CommandLineApplication Application { get; set; }

        private MvcServiceProvider MvcServiceProvider { get; set; }

        private CompilationOptions Options { get; set; }

        private string ProjectPath { get; set; }

        public void Configure(CommandLineApplication app)
        {
            Application = app;
            Options = new CompilationOptions(app);

            app.OnExecute(() => Execute());
        }

        private int Execute()
        {
            if (!ParseArguments())
            {
                return 1;
            }

            MvcServiceProvider = new MvcServiceProvider(
                ProjectPath,
                Options.ApplicationNameOption.Value(),
                Options.ContentRootOption.Value(),
                Options.ConfigureCompilationType.Value());

            var results = GenerateCode();
            var success = true;

            foreach (var result in results)
            {
                if (result.CSharpDocument.Diagnostics.Count > 0)
                {
                    success = false;
                    foreach (var error in result.CSharpDocument.Diagnostics)
                    {
                        Application.Error.WriteLine($"{result.ViewFileInfo.FullPath} ({error.Span.LineIndex}): {error.GetMessage()}");
                    }
                }
            }

            if (!success)
            {
                return 1;
            }

            var precompileAssemblyName = $"{Options.ApplicationName}.PrecompiledViews";
            var compilation = CompileViews(results, precompileAssemblyName);
            var resources = GetResources(results);

            var assemblyPath = Path.Combine(Options.OutputPath, precompileAssemblyName + ".dll");
            var emitResult = EmitAssembly(
                compilation,
                MvcServiceProvider.Compiler.EmitOptions,
                assemblyPath,
                resources);

            if (!emitResult.Success)
            {
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    Application.Error.WriteLine(CSharpDiagnosticFormatter.Instance.Format(diagnostic));
                }

                return 1;
            }

            return 0;
        }

        private ResourceDescription[] GetResources(ViewCompilationInfo[] results)
        {
            if (!Options.EmbedViewSourcesOption.HasValue())
            {
                return new ResourceDescription[0];
            }

            var resources = new ResourceDescription[results.Length];
            for (var i = 0; i < results.Length; i++)
            {
                var fileInfo = results[i].ViewFileInfo;

                resources[i] = new ResourceDescription(
                    fileInfo.ViewEnginePath,
                    fileInfo.CreateReadStream,
                    isPublic: true);
            }

            return resources;
        }

        public EmitResult EmitAssembly(
            CSharpCompilation compilation,
            EmitOptions emitOptions,
            string assemblyPath,
            ResourceDescription[] resources)
        {
            EmitResult emitResult;
            using (var assemblyStream = new MemoryStream())
            {
                using (var pdbStream = new MemoryStream())
                {
                    emitResult = compilation.Emit(
                        assemblyStream,
                        pdbStream,
                        manifestResources: resources,
                        options: emitOptions);

                    if (emitResult.Success)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(assemblyPath));
                        var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
                        assemblyStream.Position = 0;
                        pdbStream.Position = 0;

                        // Avoid writing to disk unless the compilation is successful.
                        using (var assemblyFileStream = File.OpenWrite(assemblyPath))
                        {
                            assemblyStream.CopyTo(assemblyFileStream);
                        }

                        using (var pdbFileStream = File.OpenWrite(pdbPath))
                        {
                            pdbStream.CopyTo(pdbFileStream);
                        }
                    }
                }
            }

            return emitResult;
        }

        private CSharpCompilation CompileViews(ViewCompilationInfo[] results, string assemblyname)
        {
            var compiler = MvcServiceProvider.Compiler;
            var compilation = compiler.CreateCompilation(assemblyname);
            var syntaxTrees = new SyntaxTree[results.Length];

            Parallel.For(0, results.Length, ParalellOptions, i =>
            {
                var result = results[i];
                var sourceText = SourceText.From(result.CSharpDocument.GeneratedCode, Encoding.UTF8);
                var fileInfo = result.ViewFileInfo;
                var syntaxTree = compiler.CreateSyntaxTree(sourceText)
                    .WithFilePath(fileInfo.FullPath ?? fileInfo.ViewEnginePath);
                syntaxTrees[i] = syntaxTree;
            });

            compilation = compilation.AddSyntaxTrees(syntaxTrees);

            // Post process the compilation - run ExpressionRewritter and any user specified callbacks.            
            compilation = ExpressionRewriter.Rewrite(compilation);
            var compilationContext = new RoslynCompilationContext(compilation);
            MvcServiceProvider.ViewEngineOptions.CompilationCallback(compilationContext);
            compilation  = AssemblyMetadataGenerator.AddAssemblyMetadata(
                compiler, 
                compilationContext.Compilation,
                Options);
            
            return compilation;
        }

        private bool ParseArguments()
        {
            ProjectPath = Options.ProjectArgument.Value;
            if (string.IsNullOrEmpty(ProjectPath))
            {
                Application.Error.WriteLine("Project path not specified.");
                return false;
            }

            if (!Options.OutputPathOption.HasValue())
            {
                Application.Error.WriteLine($"Option {CompilationOptions.OutputPathTemplate} does not specify a value.");
                return false;
            }

            if (!Options.ApplicationNameOption.HasValue())
            {
                Application.Error.WriteLine($"Option {CompilationOptions.ApplicationNameTemplate} does not specify a value.");
                return false;
            }

            if (!Options.ContentRootOption.HasValue())
            {
                Application.Error.WriteLine($"Option {CompilationOptions.ContentRootTemplate} does not specify a value.");
                return false;
            }

            return true;
        }

        private ViewCompilationInfo[] GenerateCode()
        {
            var files = GetRazorFiles();
            var results = new ViewCompilationInfo[files.Count];
            Parallel.For(0, results.Length, ParalellOptions, i =>
            {
                var fileInfo = files[i];
                var templateEngine = MvcServiceProvider.TemplateEngine;
                ViewCompilationInfo compilationInfo;
                using (var fileStream = fileInfo.CreateReadStream())
                {
                    var csharpDocument = templateEngine.GenerateCode(fileInfo.ViewEnginePath);
                    compilationInfo = new ViewCompilationInfo(fileInfo, csharpDocument);
                }

                results[i] = compilationInfo;
            });

            return results;
        }

        private List<ViewFileInfo> GetRazorFiles()
        {
            var contentRoot = Options.ContentRootOption.Value();
            var viewFiles = Options.ViewsToCompile;
            var viewFileInfo = new List<ViewFileInfo>(viewFiles.Count);
            var trimLength = contentRoot.EndsWith("/") ? contentRoot.Length - 1 : contentRoot.Length;

            for (var i = 0; i < viewFiles.Count; i++)
            {
                var fullPath = viewFiles[i];
                if (fullPath.StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var viewEnginePath = fullPath.Substring(trimLength).Replace('\\', '/');
                    viewFileInfo.Add(new ViewFileInfo(fullPath, viewEnginePath));
                }
            }

            return viewFileInfo;
        }
    }
}
