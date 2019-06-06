// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc
{
    public class MvcDiagnosticAnalyzerRunner : DiagnosticAnalyzerRunner
    {
        public MvcDiagnosticAnalyzerRunner(DiagnosticAnalyzer analyzer)
        {
            Analyzer = analyzer;
        }

        public DiagnosticAnalyzer Analyzer { get; }

        public Task<Diagnostic[]> GetDiagnosticsAsync(string source)
        {
            return GetDiagnosticsAsync(sources: new[] { source }, Analyzer, Array.Empty<string>());
        }

        public Task<Diagnostic[]> GetDiagnosticsAsync(Project project)
        {
            return GetDiagnosticsAsync(new[] { project }, Analyzer, Array.Empty<string>());
        }
    }
}
