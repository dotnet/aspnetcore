// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorSyntaxTreePhase : RazorEnginePhaseBase, IRazorSyntaxTreePhase
    {
        public IRazorSyntaxTreePass[] Passes { get; private set; }

        protected override void OnIntialized()
        {
            Passes = Engine.Features.OfType<IRazorSyntaxTreePass>().OrderBy(p => p.Order).ToArray();
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency(syntaxTree);
            
            foreach (var pass in Passes)
            {
                syntaxTree = pass.Execute(codeDocument, syntaxTree);
            }

            codeDocument.SetSyntaxTree(syntaxTree);
        }
    }
}