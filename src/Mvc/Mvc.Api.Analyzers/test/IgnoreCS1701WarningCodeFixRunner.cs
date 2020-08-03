// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class IgnoreCS1701WarningCodeFixRunner : CodeFixRunner
    {
        protected override CompilationOptions ConfigureCompilationOptions(CompilationOptions options)
        {
            options = base.ConfigureCompilationOptions(options);
            return options.WithSpecificDiagnosticOptions(new[] { "CS1701" }.ToDictionary(c => c, _ => ReportDiagnostic.Suppress));
        }
    }
}