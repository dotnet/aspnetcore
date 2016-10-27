// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorParsingPhase : RazorEnginePhaseBase, IRazorParsingPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = RazorSyntaxTree.Parse(codeDocument.Source);
            codeDocument.SetSyntaxTree(syntaxTree);
        }
    }
}
