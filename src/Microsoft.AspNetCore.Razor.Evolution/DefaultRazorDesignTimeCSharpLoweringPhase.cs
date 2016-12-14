// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorDesignTimeCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var irDocument = codeDocument.GetIRDocument();
            ThrowForMissingDependency(irDocument);

            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency(syntaxTree);

            // Render design time CSharp
        }
    }
}
