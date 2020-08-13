// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class ConfigureReturnTypeAnalyzer
    {
        private readonly StartupAnalysisBuilder _context;

        public ConfigureReturnTypeAnalyzer(StartupAnalysisBuilder context)
        {
            _context = context;
        }

        public void AnalyzeConfigureMethod(OperationBlockStartAnalysisContext context)
        {
            var configureMethod = (IMethodSymbol)context.OwningSymbol;

            if (!configureMethod.ReturnsVoid)
            {
                // TODO: ReportDiagnostic
            }
        }
    }
}
