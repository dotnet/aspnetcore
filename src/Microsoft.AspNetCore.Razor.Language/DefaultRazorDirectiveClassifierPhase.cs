// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorDirectiveClassifierPhase : RazorEnginePhaseBase, IRazorDirectiveClassifierPhase
    {
        public IRazorDirectiveClassifierPass[] Passes { get; private set; }

        protected override void OnIntialized()
        {
            Passes = Engine.Features.OfType<IRazorDirectiveClassifierPass>().OrderBy(p => p.Order).ToArray();
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var irDocument = codeDocument.GetIRDocument();
            ThrowForMissingDependency(irDocument);

            foreach (var pass in Passes)
            {
                pass.Execute(codeDocument, irDocument);
            }

            codeDocument.SetIRDocument(irDocument);
        }
    }
}
