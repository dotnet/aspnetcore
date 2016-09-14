// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation.Design.Internal
{
    public class PrecompileRunCommand
    {
        public static readonly string ApplicationNameTemplate = "--application-name";
        public static readonly string OutputPathTemplate = "--output-path";
        private static readonly ParallelOptions ParalellOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4
        };

        private CommandLineApplication Application { get; set; }

        private CommandOption OutputPathOption { get; set; }

        private CommandOption ApplicationNameOption { get; set; }

        private MvcServiceProvider MvcServiceProvider { get; set; }

        private CommonOptions Options { get; } = new CommonOptions();

        private StrongNameOptions StrongNameOptions { get; } = new StrongNameOptions();

        private string ProjectPath { get; set; }

        public void Configure(CommandLineApplication app)
        {
            Application = app;
            Options.Configure(app);
            StrongNameOptions.Configure(app);

            OutputPathOption = app.Option(
               OutputPathTemplate,
                "Path to the emit the precompiled assembly to.",
                CommandOptionType.SingleValue);

            ApplicationNameOption = app.Option(
                ApplicationNameTemplate,
                "Name of the application to produce precompiled assembly for.",
                CommandOptionType.SingleValue);

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
                ApplicationNameOption.Value(),
                Options.ContentRootOption.Value(),
                Options.ConfigureCompilationType.Value());

            Application.Out.WriteLine("Running Razor view precompilation.");

            var stopWatch = Stopwatch.StartNew();
            var results = GenerateCode();
            var success = true;
            foreach (var result in results)
            {
                if (!result.GeneratorResults.Success)
                {
                    success = false;
                    foreach (var error in result.GeneratorResults.ParserErrors)
                    {
                        Application.Error.WriteLine($"{error.Location.FilePath} ({error.Location.LineIndex}): {error.Message}");
                    }
                }
            }

            if (!success)
            {
                return 1;
            }

            var precompileAssemblyName = $"{ApplicationNameOption.Value()}{ViewsFeatureProvider.PrecompiledViewsAssemblySuffix}";
            var compilation = CompileViews(results, precompileAssemblyName);
            var resources = GetResources(results);

            var assemblyPath = Path.Combine(OutputPathOption.Value(), precompileAssemblyName + ".dll");
            var emitResult = EmitAssembly(compilation, assemblyPath, resources);

            if (!emitResult.Success)
            {
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    Application.Error.WriteLine(CSharpDiagnosticFormatter.Instance.Format(diagnostic));
                }

                return 1;
            }

            stopWatch.Stop();
            Application.Out.WriteLine($"Precompiled views emitted to {assemblyPath}.");
            Application.Out.WriteLine($"Successfully compiled {results.Length} Razor views in {stopWatch.ElapsedMilliseconds}ms.");
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
                var fileInfo = results[i].RelativeFileInfo;

                resources[i] = new ResourceDescription(
                    fileInfo.RelativePath.Replace('\\', '/'),
                    fileInfo.FileInfo.CreateReadStream,
                    isPublic: true);
            }

            return resources;
        }

        private EmitResult EmitAssembly(
            CSharpCompilation compilation,
            string assemblyPath,
            ResourceDescription[] resources)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assemblyPath));

            EmitResult emitResult;
            using (var assemblyStream = File.OpenWrite(assemblyPath))
            {
                using (var pdbStream = File.OpenWrite(Path.ChangeExtension(assemblyPath, ".pdb")))
                {
                    emitResult = compilation.Emit(
                        assemblyStream,
                        pdbStream,
                        manifestResources: resources,
                        options: MvcServiceProvider.Compiler.EmitOptions);
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
                var sourceText = SourceText.From(result.GeneratorResults.GeneratedCode, Encoding.UTF8);
                var fileInfo = result.RelativeFileInfo;
                var syntaxTree = compiler.CreateSyntaxTree(sourceText)
                    .WithFilePath(fileInfo.FileInfo.PhysicalPath ?? fileInfo.RelativePath);
                syntaxTrees[i] = syntaxTree;
            });

            compilation = compilation.AddSyntaxTrees(syntaxTrees);
            Parallel.For(0, results.Length, ParalellOptions, i =>
            {
                results[i].TypeName = ReadTypeInfo(compilation, syntaxTrees[i]);
            });

            // Post process the compilation - run ExpressionRewritter and any user specified callbacks.            
            compilation = ExpressionRewriter.Rewrite(compilation);
            var compilationContext = new RoslynCompilationContext(compilation);
            MvcServiceProvider.ViewEngineOptions.CompilationCallback(compilationContext);
            compilation = compilationContext.Compilation;

            var codeGenerator = new ViewInfoContainerCodeGenerator(compiler, compilation);
            codeGenerator.AddViewFactory(results);

            var assemblyName = new AssemblyName(ApplicationNameOption.Value());
            assemblyName = Assembly.Load(assemblyName).GetName();
            codeGenerator.AddAssemblyMetadata(assemblyName, StrongNameOptions);

            return codeGenerator.Compilation;
        }

        private bool ParseArguments()
        {
            ProjectPath = Options.ProjectArgument.Value;
            if (string.IsNullOrEmpty(ProjectPath))
            {
                Application.Error.WriteLine("Project path not specified.");
                return false;
            }

            if (!OutputPathOption.HasValue())
            {
                Application.Error.WriteLine($"Option {OutputPathTemplate} does not specify a value.");
                return false;
            }

            if (!ApplicationNameOption.HasValue())
            {
                Application.Error.WriteLine($"Option {ApplicationNameTemplate} does not specify a value.");
                return false;
            }

            if (!Options.ContentRootOption.HasValue())
            {
                Application.Error.WriteLine($"Option {CommonOptions.ContentRootTemplate} does not specify a value.");
                return false;
            }

            return true;
        }

        private ViewCompilationInfo[] GenerateCode()
        {
            var files = new List<RelativeFileInfo>();
            GetRazorFiles(MvcServiceProvider.FileProvider, files, root: string.Empty);
            var results = new ViewCompilationInfo[files.Count];
            Parallel.For(0, results.Length, ParalellOptions, i =>
            {
                var fileInfo = files[i];
                using (var fileStream = fileInfo.FileInfo.CreateReadStream())
                {
                    var result = MvcServiceProvider.Host.GenerateCode(fileInfo.RelativePath, fileStream);
                    results[i] = new ViewCompilationInfo(fileInfo, result);
                }
            });

            return results;
        }

        private static void GetRazorFiles(IFileProvider fileProvider, List<RelativeFileInfo> razorFiles, string root)
        {
            foreach (var fileInfo in fileProvider.GetDirectoryContents(root))
            {
                var relativePath = Path.Combine(root, fileInfo.Name);
                if (fileInfo.IsDirectory)
                {
                    GetRazorFiles(fileProvider, razorFiles, relativePath);
                }
                else if (fileInfo.Name.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
                {
                    razorFiles.Add(new RelativeFileInfo(fileInfo, relativePath));
                }
            }
        }

        private string ReadTypeInfo(CSharpCompilation compilation, SyntaxTree syntaxTree)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
            var classDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var declaration in classDeclarations)
            {
                var typeSymbol = semanticModel.GetDeclaredSymbol(declaration);
                if (typeSymbol.ContainingType == null && typeSymbol.DeclaredAccessibility == Accessibility.Public)
                {
                    return typeSymbol.ToDisplayString();
                }
            }

            return null;
        }
    }
}
