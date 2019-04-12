// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class StartupAnalysisContext
    {
        public StartupAnalysisContext(OperationBlockStartAnalysisContext operationBlockStartAnalysisContext, StartupSymbols startupSymbols)
        {
            OperationBlockStartAnalysisContext = operationBlockStartAnalysisContext;
            StartupSymbols = startupSymbols;
        }

        public OperationBlockStartAnalysisContext OperationBlockStartAnalysisContext { get; }

        public StartupSymbols StartupSymbols { get; }
    }
}
