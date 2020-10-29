// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class RazorSourceGenerationContext
    {
        public string RootNamespace { get; private set; }

        public IReadOnlyList<RazorInputItem> RazorFiles { get; private set; }

        public IReadOnlyList<RazorInputItem> CshtmlFiles { get; private set; }

        public VirtualRazorProjectFileSystem FileSystem { get; private set; }

        public RazorConfiguration Configuration { get; private set; }

        public bool DesignTimeBuild { get; private set; }

        public string RefsTagHelperOutputCachePath { get; private set; }

        /// <summary>
        /// Gets a flag that determines if the source generator waits for the debugger to attach.
        /// <para>
        /// To configure this using MSBuild, use the <c>_RazorSourceGeneratorDebug</c> property.
        /// For instance <c>dotnet msbuild /p:_RazorSourceGeneratorDebug=true</c>
        /// </para>
        /// </summary>
        public bool WaitForDebugger { get; private set; }

        /// <summary>
        /// Gets a flag that determines if generated outut is to be written to disk.
        /// Primarily meant for tests and debugging.
        /// <para>
        /// To configure this using MSBuild, use the <c>_RazorSourceGeneratorWriteGeneratedOutput</c> property.
        /// For instance <c>dotnet msbuild /p:_RazorSourceGeneratorWriteGeneratedOutput=true</c>
        /// </para>
        /// </summary>
        public bool WriteGeneratedContent { get; private set; }

        public static RazorSourceGenerationContext Create(GeneratorExecutionContext context)
        {
            var globalOptions = context.AnalyzerConfigOptions.GlobalOptions;

            if (!globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
            {
                rootNamespace = "ASP";
            }

            globalOptions.TryGetValue("build_property.DesignTimeBuild", out var designTimeBuild);
            if (!globalOptions.TryGetValue("build_property._RazorReferenceAssemblyTagHelpersOutputPath", out var refsTagHelperOutputCachePath))
            {
                throw new InvalidOperationException("_RazorReferenceAssemblyTagHelpersOutputPath is not specified.");
            }

            if (!globalOptions.TryGetValue("build_property.RazorLangVersion", out var razorLanguageVersionString) ||
                !RazorLanguageVersion.TryParse(razorLanguageVersionString, out var razorLanguageVersion))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RazorDiagnostics.InvalidRazorLangVersionDescriptor,
                    Location.None,
                    razorLanguageVersionString));

                return null;
            }

            if (!globalOptions.TryGetValue("build_property.RazorConfiguration", out var configurationName))
            {
                configurationName = "default";
            }

            globalOptions.TryGetValue("build_property._RazorSourceGeneratorDebug", out var waitForDebugger);
            globalOptions.TryGetValue("build_property._RazorSourceGeneratorWriteGeneratedOutput", out var writeOutput);

            var razorConfiguration = RazorConfiguration.Create(razorLanguageVersion, configurationName, Enumerable.Empty<RazorExtension>());
            var (razorFiles, cshtmlFiles) = GetRazorInputs(context);
            var fileSystem = GetVirtualFileSystem(razorFiles, cshtmlFiles);

            return new RazorSourceGenerationContext
            {
                RootNamespace = rootNamespace,
                Configuration = razorConfiguration,
                FileSystem = fileSystem,
                RazorFiles = razorFiles,
                CshtmlFiles = cshtmlFiles,
                DesignTimeBuild = designTimeBuild == "true",
                RefsTagHelperOutputCachePath = refsTagHelperOutputCachePath,
                WaitForDebugger = waitForDebugger == "true",
                WriteGeneratedContent = writeOutput == "true",
            };
        }

        private static VirtualRazorProjectFileSystem GetVirtualFileSystem(IReadOnlyList<RazorInputItem> razorFiles, IReadOnlyList<RazorInputItem> cshtmlFiles)
        {
            var fileSystem = new VirtualRazorProjectFileSystem();
            for (var i = 0; i < razorFiles.Count; i++)
            {
                var item = razorFiles[i];
                fileSystem.Add(new SourceGeneratorProjectItem(
                    basePath: "/",
                    filePath: item.NormalizedPath,
                    relativePhysicalPath: item.RelativePath,
                    fileKind: FileKinds.Component,
                    item.AdditionalText,
                    cssScope: item.CssScope));
            }

            for (var i = 0; i < cshtmlFiles.Count; i++)
            {
                var item = cshtmlFiles[i];
                fileSystem.Add(new SourceGeneratorProjectItem(
                    basePath: "/",
                    filePath: item.NormalizedPath,
                    relativePhysicalPath: item.RelativePath,
                    fileKind: FileKinds.Legacy,
                    item.AdditionalText,
                    cssScope: item.CssScope));
            }

            return fileSystem;
        }

        private static (IReadOnlyList<RazorInputItem> razorFiles, IReadOnlyList<RazorInputItem> cshtmlFiles) GetRazorInputs(GeneratorExecutionContext context)
        {
            List<RazorInputItem> razorFiles = null;
            List<RazorInputItem> cshtmlFiles = null;

            foreach (var item in context.AdditionalFiles)
            {
                var path = item.Path;
                var isComponent = path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
                var isRazorView = !isComponent && path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);

                if (!isComponent && !isRazorView)
                {
                    continue;
                }

                var options = context.AnalyzerConfigOptions.GetOptions(item);
                if (!options.TryGetValue("build_metadata.AdditionalFiles.TargetPath", out var relativePath))
                {
                    throw new InvalidOperationException($"TargetPath is not specified for additional file '{item.Path}.");
                }

                options.TryGetValue("build_metadata.AdditionalFiles.CssScope", out var cssScope);

                string generatedDeclarationPath = null;

                options.TryGetValue("build_metadata.AdditionalFiles.GeneratedOutputFullPath", out var generatedOutputPath);
                if (isComponent)
                {
                    options.TryGetValue("build_metadata.AdditionalFiles.GeneratedDeclarationFullPath", out generatedDeclarationPath);
                }

                var fileKind = isComponent ? FileKinds.GetComponentFileKindFromFilePath(item.Path) : FileKinds.Legacy;

                var inputItem = new RazorInputItem(item, relativePath, fileKind, generatedOutputPath, generatedDeclarationPath, cssScope);

                if (isComponent)
                {
                    razorFiles ??= new();
                    razorFiles.Add(inputItem);
                }
                else
                {
                    cshtmlFiles ??= new();
                    cshtmlFiles.Add(inputItem);
                }
            }

            return (
                (IReadOnlyList<RazorInputItem>)razorFiles ?? Array.Empty<RazorInputItem>(),
                (IReadOnlyList<RazorInputItem>)cshtmlFiles ?? Array.Empty<RazorInputItem>()
            );
        }

    }
}
