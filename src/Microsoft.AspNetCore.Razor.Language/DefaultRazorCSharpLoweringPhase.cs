// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var irDocument = codeDocument.GetDocumentIntermediateNode();
            ThrowForMissingDocumentDependency(irDocument);

            var target = irDocument.Target;
            if (target == null)
            {
                var message = Resources.FormatDocumentMissingTarget(
                    irDocument.DocumentKind,
                    nameof(CodeTarget),
                    nameof(DocumentIntermediateNode.Target));
                throw new InvalidOperationException(message);
            }

            var context = CodeRenderingContext.Create(codeDocument, irDocument.Options);
            var documentWriter = target.CreateWriter(context);
            documentWriter.WriteDocument(irDocument);

            var diagnostics = new List<RazorDiagnostic>();
            diagnostics.AddRange(irDocument.GetAllDiagnostics());
            diagnostics.AddRange(context.Diagnostics);

            var lineMappings = context.GetLineMappings();
            var csharpDocument = RazorCSharpDocument.Create(
                context.CodeWriter.GenerateCode(),
                irDocument.Options,
                diagnostics,
                lineMappings);
            codeDocument.SetCSharpDocument(csharpDocument);
        }
    }
}
