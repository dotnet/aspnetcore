// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using DependencyContextCompilationOptions = Microsoft.Extensions.DependencyModel.CompilationOptions;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    internal class CSharpCompiler
    {
        private readonly RazorReferenceManager _referenceManager;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private bool _optionsInitialized;
        private CSharpParseOptions _parseOptions;
        private CSharpCompilationOptions _compilationOptions;
        private EmitOptions _emitOptions;
        private bool _emitPdb;

        public CSharpCompiler(RazorReferenceManager manager, IWebHostEnvironment hostingEnvironment)
        {
            _referenceManager = manager ?? throw new ArgumentNullException(nameof(manager));
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        public virtual CSharpParseOptions ParseOptions
        {
            get
            {
                EnsureOptions();
                return _parseOptions;
            }
        }

        public virtual CSharpCompilationOptions CSharpCompilationOptions
        {
            get
            {
                EnsureOptions();
                return _compilationOptions;
            }
        }

        public virtual bool EmitPdb
        {
            get
            {
                EnsureOptions();
                return _emitPdb;
            }
        }

        public virtual EmitOptions EmitOptions
        {
            get
            {
                EnsureOptions();
                return _emitOptions;
            }
        }

        public SyntaxTree CreateSyntaxTree(SourceText sourceText)
        {
            return CSharpSyntaxTree.ParseText(
                sourceText,
                options: ParseOptions);
        }

        public CSharpCompilation CreateCompilation(string assemblyName)
        {
            return CSharpCompilation.Create(
                assemblyName,
                options: CSharpCompilationOptions,
                references: _referenceManager.CompilationReferences);
        }

        // Internal for unit testing.
        protected internal virtual DependencyContextCompilationOptions GetDependencyContextCompilationOptions()
        {
            if (!string.IsNullOrEmpty(_hostingEnvironment.ApplicationName))
            {
                var applicationAssembly = Assembly.Load(new AssemblyName(_hostingEnvironment.ApplicationName));
                var dependencyContext = DependencyContext.Load(applicationAssembly);
                if (dependencyContext?.CompilationOptions != null)
                {
                    return dependencyContext.CompilationOptions;
                }
            }

            return DependencyContextCompilationOptions.Default;
        }

        private void EnsureOptions()
        {
            if (!_optionsInitialized)
            {
                var dependencyContextOptions = GetDependencyContextCompilationOptions();
                _parseOptions = GetParseOptions(_hostingEnvironment, dependencyContextOptions);
                _compilationOptions = GetCompilationOptions(_hostingEnvironment, dependencyContextOptions);
                _emitOptions = GetEmitOptions(dependencyContextOptions);

                _optionsInitialized = true;
            }
        }

        private EmitOptions GetEmitOptions(DependencyContextCompilationOptions dependencyContextOptions)
        {
            // Assume we're always producing pdbs unless DebugType = none
            _emitPdb = true;
            DebugInformationFormat debugInformationFormat;
            if (string.IsNullOrEmpty(dependencyContextOptions.DebugType))
            {
                debugInformationFormat = DebugInformationFormat.PortablePdb;
            }
            else
            {
                // Based on https://github.com/dotnet/roslyn/blob/1d28ff9ba248b332de3c84d23194a1d7bde07e4d/src/Compilers/CSharp/Portable/CommandLine/CSharpCommandLineParser.cs#L624-L640
                switch (dependencyContextOptions.DebugType.ToLower())
                {
                    case "none":
                        // There isn't a way to represent none in DebugInformationFormat.
                        // We'll set EmitPdb to false and let callers handle it by setting a null pdb-stream.
                        _emitPdb = false;
                        return new EmitOptions();
                    case "portable":
                        debugInformationFormat = DebugInformationFormat.PortablePdb;
                        break;
                    case "embedded":
                        // Roslyn does not expose enough public APIs to produce a binary with embedded pdbs.
                        // We'll produce PortablePdb instead to continue providing a reasonable user experience.
                        debugInformationFormat = DebugInformationFormat.PortablePdb;
                        break;
                    case "full":
                    case "pdbonly":
                        debugInformationFormat = DebugInformationFormat.PortablePdb;
                        break;
                    default:
                        throw new InvalidOperationException(Resources.FormatUnsupportedDebugInformationFormat(dependencyContextOptions.DebugType));
                }
            }

            var emitOptions = new EmitOptions(debugInformationFormat: debugInformationFormat);
            return emitOptions;
        }

        private static CSharpCompilationOptions GetCompilationOptions(
            IWebHostEnvironment hostingEnvironment,
            DependencyContextCompilationOptions dependencyContextOptions)
        {
            var csharpCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            // Disable 1702 until roslyn turns this off by default
            csharpCompilationOptions = csharpCompilationOptions.WithSpecificDiagnosticOptions(
                new Dictionary<string, ReportDiagnostic>
                {
                    {"CS1701", ReportDiagnostic.Suppress}, // Binding redirects
                    {"CS1702", ReportDiagnostic.Suppress},
                    {"CS1705", ReportDiagnostic.Suppress}
                });

            if (dependencyContextOptions.AllowUnsafe.HasValue)
            {
                csharpCompilationOptions = csharpCompilationOptions.WithAllowUnsafe(
                    dependencyContextOptions.AllowUnsafe.Value);
            }

            OptimizationLevel optimizationLevel;
            if (dependencyContextOptions.Optimize.HasValue)
            {
                optimizationLevel = dependencyContextOptions.Optimize.Value ?
                    OptimizationLevel.Release :
                    OptimizationLevel.Debug;
            }
            else
            {
                optimizationLevel = hostingEnvironment.IsDevelopment() ?
                    OptimizationLevel.Debug :
                    OptimizationLevel.Release;
            }
            csharpCompilationOptions = csharpCompilationOptions.WithOptimizationLevel(optimizationLevel);

            if (dependencyContextOptions.WarningsAsErrors.HasValue)
            {
                var reportDiagnostic = dependencyContextOptions.WarningsAsErrors.Value ?
                    ReportDiagnostic.Error :
                    ReportDiagnostic.Default;
                csharpCompilationOptions = csharpCompilationOptions.WithGeneralDiagnosticOption(reportDiagnostic);
            }

            return csharpCompilationOptions;
        }

        private static CSharpParseOptions GetParseOptions(
            IWebHostEnvironment hostingEnvironment,
            DependencyContextCompilationOptions dependencyContextOptions)
        {
            var configurationSymbol = hostingEnvironment.IsDevelopment() ? "DEBUG" : "RELEASE";
            var defines = dependencyContextOptions.Defines.Concat(new[] { configurationSymbol });

            var parseOptions = new CSharpParseOptions(preprocessorSymbols: defines);

            if (string.IsNullOrEmpty(dependencyContextOptions.LanguageVersion))
            {
                // If the user does not specify a LanguageVersion, assume CSharp 8.0. This matches the language version Razor 3.0 targets by default.
                parseOptions = parseOptions.WithLanguageVersion(LanguageVersion.CSharp8);
            }
            else if (LanguageVersionFacts.TryParse(dependencyContextOptions.LanguageVersion, out var languageVersion))
            {
                parseOptions = parseOptions.WithLanguageVersion(languageVersion);
            }
            else
            {
                Debug.Fail($"LanguageVersion {languageVersion} specified in the deps file could not be parsed.");
            }

            return parseOptions;
        }
    }
}
