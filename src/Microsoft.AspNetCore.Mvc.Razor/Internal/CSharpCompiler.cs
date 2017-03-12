// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class CSharpCompiler
    {
        private readonly CSharpCompilationOptions _compilationOptions;
        private readonly CSharpParseOptions _parseOptions;
        private readonly RazorReferenceManager _referenceManager;
        private readonly DebugInformationFormat _pdbFormat =
#if NET46
            SymbolsUtility.SupportsFullPdbGeneration() ?
                DebugInformationFormat.Pdb :
                DebugInformationFormat.PortablePdb;
#elif NETSTANDARD1_6
            DebugInformationFormat.PortablePdb;
#else
#error target frameworks need to be updated.
#endif

        public CSharpCompiler(RazorReferenceManager manager, IOptions<RazorViewEngineOptions> optionsAccessor)
        {
            _referenceManager = manager;
            _compilationOptions = optionsAccessor.Value.CompilationOptions;
            _parseOptions = optionsAccessor.Value.ParseOptions;
            EmitOptions = new EmitOptions(debugInformationFormat: _pdbFormat);
        }

        public EmitOptions EmitOptions { get; }

        public SyntaxTree CreateSyntaxTree(SourceText sourceText)
        {
            return CSharpSyntaxTree.ParseText(
                sourceText,
                options: _parseOptions);
        }

        public CSharpCompilation CreateCompilation(string assemblyName)
        {
            return CSharpCompilation.Create(
                assemblyName,
                options: _compilationOptions,
                references: _referenceManager.CompilationReferences);
        }
    }
}
