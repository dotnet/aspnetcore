// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public interface IRazorIROptimizationPass : IRazorEngineFeature
    {
        int Order { get; }

        void Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument);
    }
}
